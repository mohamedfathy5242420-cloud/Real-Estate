using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;
public sealed class ViewingChangeService(ICrmCustomerRepository staffScope, ICustomerRequestScope ownerScope,
    IViewingCoordinationRepository repository, IViewingLifecycleRepository resources, IViewingRepository history,
    ICommandExecutor executor, ICurrentUser user, TimeProvider clock) : IViewingChangeService
{
    private Guid? Actor(bool staff) => user.IsAuthenticated && user.AccountId is { } actor && actor != Guid.Empty &&
        (staff ? user.IsInRole(AppRoles.SalesManager) || user.IsInRole(AppRoles.SalesEmployee) : user.IsInRole(AppRoles.Customer)) ? actor : null;
    public Task<ViewingResult<ViewingReply>> CancelOwnAsync(Guid id, CancelViewingDto request, string key, CancellationToken ct) =>
        ExecuteAsync(null, id, request.Version, request.Reason, "Customer", null, key, ct);
    public Task<ViewingResult<ViewingReply>> ChangeProposalAsync(Guid id, ChangeViewingProposalDto request, string key, CancellationToken ct) =>
        request.ProposedStartUtc == default || request.ProposedStartUtc.Offset != TimeSpan.Zero
            ? Task.FromResult(new ViewingResult<ViewingReply>(CommandStatus.Invalid))
            : ExecuteAsync(null, id, request.Version, request.Reason, null, request.ProposedStartUtc, key, ct);
    public Task<ViewingResult<ViewingReply>> CancelStaffAsync(Guid customerId, Guid id, CancelStaffViewingDto request, string key, CancellationToken ct) =>
        request.CancellationKind is not ("Broker" or "Developer")
            ? Task.FromResult(new ViewingResult<ViewingReply>(CommandStatus.Invalid))
            : ExecuteAsync(customerId, id, request.Version, request.Reason, request.CancellationKind, null, key, ct);
    private async Task<ViewingResult<ViewingReply>> ExecuteAsync(Guid? customerId, Guid id, long version, string reason,
        string? cancellationKind, DateTimeOffset? proposal, string key, CancellationToken ct)
    {
        if (Actor(customerId.HasValue) is not { } actor) return new(CommandStatus.Forbidden);
        if (id == Guid.Empty || customerId == Guid.Empty || version <= 0 || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > Viewing.ChangeReasonMaxLength)
            return new(CommandStatus.Invalid);
        Viewing? viewing = null;
        var operation = proposal.HasValue ? "Viewing.ChangeProposal" : customerId.HasValue ? "Viewing.CancelStaff" : "Viewing.CancelOwn";
        var result = await executor.ExecuteAsync(operation, key, JsonSerializer.SerializeToElement(new { customerId, id, version,
            reason = reason.Trim(), cancellationKind, proposedStartUtc = proposal?.UtcDateTime }),
            async token =>
            {
                viewing = null; Guid scopeId;
                if (customerId is { } staffCustomer)
                {
                    var access = await staffScope.LockForSalesAsync(staffCustomer, actor, false, token);
                    if (access.Permission != CommandPermission.Allowed) return access.Permission;
                    scopeId = access.Customer!.Id;
                }
                else
                {
                    var access = await ownerScope.LockOwnedAsync(actor, token);
                    if (access.Permission != CommandPermission.Allowed) return access.Permission;
                    scopeId = access.Customer!.Id;
                }
                viewing = await repository.LockAsync(scopeId, id, token);
                return viewing is null ? CommandPermission.NotFound : CommandPermission.Allowed;
            },
            async (context, token) =>
            {
                var v = viewing ?? throw new InvalidOperationException("Current scoped Viewing required.");
                if (!v.CanChange(version, clock.GetUtcNow().UtcDateTime)) return new CommandEffect(CommandStatus.Conflict);
                var oldEmployee = v.ConfirmedEmployeeId;
                if (v.Status == Viewing.Confirmed && (oldEmployee is null || !await resources.LockReleasedResourcesAsync(v.UnitId, oldEmployee.Value, token)))
                    return new CommandEffect(CommandStatus.Conflict);
                var now = clock.GetUtcNow().UtcDateTime;
                if (!v.CanChange(version, now)) return new CommandEffect(CommandStatus.Conflict);
                ViewingEvent record;
                try
                {
                    if (proposal is { } start)
                    {
                        var previous = v.ChangeProposal(version, start.UtcDateTime, reason, now, context.ActorAccountId);
                        record = ViewingEvent.Rescheduled(context.CommandId, v, previous, context.ActorAccountId, reason);
                    }
                    else { v.Cancel(version, cancellationKind!, reason, now, context.ActorAccountId); record = ViewingEvent.Cancellation(context.CommandId, v); }
                }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                catch (InvalidOperationException) { return new CommandEffect(CommandStatus.Conflict); }
                history.AddEvent(record);
                context.Emit(proposal.HasValue ? "Viewing.Rescheduled" : "Viewing.Cancelled", 1, v.CustomerId,
                    JsonSerializer.SerializeToElement(new { viewingId = v.Id, customerId = v.CustomerId, unitId = v.UnitId,
                        employeeId = oldEmployee, status = v.Status, proposedStartUtc = v.CurrentProposedStartUtc, proposedEndUtc = v.CurrentProposedEndUtc, version = v.Version }));
                return new CommandEffect(CommandStatus.Success, new CommandOutcome("Viewing", v.Id, v.Version));
            }, ct);
        return result.Status == CommandStatus.Success ? new(CommandStatus.Success, new(result.Outcome!.ResourceId, result.Outcome.ResourceVersion, result.IsReplay)) : new(result.Status);
    }
}
