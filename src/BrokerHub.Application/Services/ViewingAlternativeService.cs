using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;
public sealed class ViewingAlternativeService(ICrmCustomerRepository staffScope, ICustomerRequestScope ownerScope,
    IViewingCoordinationRepository repository, IViewingRepository history, ICommandExecutor executor,
    ICurrentUser user, TimeProvider clock) : IViewingAlternativeService
{
    private Guid? Actor(string role) => user.IsAuthenticated && user.IsInRole(role) && user.AccountId is { } id && id != Guid.Empty ? id : null;
    private Guid? Staff => Actor(AppRoles.SalesManager) ?? Actor(AppRoles.SalesEmployee);
    public async Task<ViewingResult<ViewingReply>> OfferAsync(Guid customerId, Guid id, OfferViewingAlternativeDto request, string key, CancellationToken ct)
    {
        if (Staff is not { } actor) return new(CommandStatus.Forbidden);
        if (customerId == Guid.Empty || id == Guid.Empty || request.Version <= 0 || request.ProposedStartUtc == default ||
            request.ProposedEndUtc <= request.ProposedStartUtc || request.CoordinatedAtUtc == default || request.ProposedStartUtc.Offset != TimeSpan.Zero ||
            request.ProposedEndUtc.Offset != TimeSpan.Zero || request.CoordinatedAtUtc.Offset != TimeSpan.Zero ||
            string.IsNullOrWhiteSpace(request.CoordinationSource) || request.CoordinationSource.Trim().Length > Viewing.CoordinationSourceMaxLength ||
            request.CoordinationReference?.Trim().Length > Viewing.CoordinationReferenceMaxLength || string.IsNullOrWhiteSpace(request.Reason) ||
            request.Reason.Trim().Length > Viewing.AlternativeReasonMaxLength) return new(CommandStatus.Invalid);
        Customer? customer = null; Viewing? viewing = null;
        var result = await executor.ExecuteAsync("Viewing.OfferAlternative", key, JsonSerializer.SerializeToElement(new { customerId, id,
            version = request.Version, proposedStartUtc = request.ProposedStartUtc.UtcDateTime, proposedEndUtc = request.ProposedEndUtc.UtcDateTime,
            coordinatedAtUtc = request.CoordinatedAtUtc.UtcDateTime, coordinationSource = request.CoordinationSource.Trim(),
            coordinationReference = string.IsNullOrWhiteSpace(request.CoordinationReference) ? null : request.CoordinationReference.Trim(), reason = request.Reason.Trim() }),
            async token =>
            {
                customer = null; viewing = null; var access = await staffScope.LockForSalesAsync(customerId, actor, false, token);
                if (access.Permission != CommandPermission.Allowed) return access.Permission;
                customer = access.Customer; viewing = await repository.LockAsync(customerId, id, token);
                return viewing is null ? CommandPermission.NotFound : CommandPermission.Allowed;
            },
            async (context, token) =>
            {
                var v = viewing ?? throw new InvalidOperationException("Current scoped Viewing required.");
                if (v.Status is not (Viewing.PendingConfirmation or Viewing.AwaitingCustomerAcceptance) || v.Version != request.Version ||
                    v.Version == long.MaxValue || clock.GetUtcNow().UtcDateTime >= v.PendingExpiresAtUtc) return new CommandEffect(CommandStatus.Conflict);
                if (customer!.AssignedEmployeeId is not { } employee) return new CommandEffect(CommandStatus.Conflict);
                var account = await repository.LockResponsibleAccountAsync(employee, token);
                if (account is null || !await repository.LockResourcesAsync(v.UnitId, employee, account.Value, token)) return new CommandEffect(CommandStatus.Conflict);
                if (await repository.ConflictsAsync(v.UnitId, employee, request.ProposedStartUtc.UtcDateTime, request.ProposedEndUtc.UtcDateTime, token)) return new CommandEffect(CommandStatus.Conflict);
                var now = clock.GetUtcNow().UtcDateTime;
                if (now >= v.PendingExpiresAtUtc) return new CommandEffect(CommandStatus.Conflict);
                ViewingEvent record;
                try
                {
                    var old = v.OfferAlternative(request.Version, employee, request.ProposedStartUtc.UtcDateTime, request.ProposedEndUtc.UtcDateTime,
                        request.CoordinatedAtUtc.UtcDateTime, request.CoordinationSource, request.CoordinationReference, now, context.ActorAccountId);
                    record = ViewingEvent.AlternativeOffered(context.CommandId, v, old.StartUtc, old.EndUtc, request.Reason);
                }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                catch (InvalidOperationException) { return new CommandEffect(CommandStatus.Conflict); }
                history.AddEvent(record);
                context.Emit("Viewing.AlternativeOffered", 1, customerId, JsonSerializer.SerializeToElement(new { viewingId = v.Id, customerId,
                    unitId = v.UnitId, employeeId = employee, proposedStartUtc = v.CurrentProposedStartUtc, proposedEndUtc = v.CurrentProposedEndUtc, version = v.Version }));
                return new CommandEffect(CommandStatus.Success, new CommandOutcome("Viewing", v.Id, v.Version));
            }, ct);
        return Map(result);
    }
    public async Task<ViewingResult<ViewingReply>> AcceptAsync(Guid id, AcceptViewingAlternativeDto request, string key, CancellationToken ct)
    {
        if (Actor(AppRoles.Customer) is not { } actor) return new(CommandStatus.Forbidden);
        if (id == Guid.Empty || request.Version <= 0) return new(CommandStatus.Invalid);
        Customer? customer = null; Viewing? viewing = null;
        var result = await executor.ExecuteAsync("Viewing.AcceptAlternative", key, JsonSerializer.SerializeToElement(new { id, version = request.Version }),
            async token =>
            {
                customer = null; viewing = null; var access = await ownerScope.LockOwnedAsync(actor, token);
                if (access.Permission != CommandPermission.Allowed) return access.Permission;
                customer = access.Customer; viewing = await repository.LockAsync(customer!.Id, id, token);
                return viewing is null ? CommandPermission.NotFound : CommandPermission.Allowed;
            },
            async (context, token) =>
            {
                var v = viewing ?? throw new InvalidOperationException("Current owned Viewing required.");
                if (v.Status != Viewing.AwaitingCustomerAcceptance || v.Version != request.Version || v.Version == long.MaxValue ||
                    clock.GetUtcNow().UtcDateTime >= v.PendingExpiresAtUtc || v.OfferedEmployeeId is not { } employee || customer!.AssignedEmployeeId != employee)
                    return new CommandEffect(CommandStatus.Conflict);
                var account = await repository.LockResponsibleAccountAsync(employee, token);
                if (account is null || !await repository.LockResourcesAsync(v.UnitId, employee, account.Value, token)) return new CommandEffect(CommandStatus.Conflict);
                if (await repository.ConflictsAsync(v.UnitId, employee, v.CurrentProposedStartUtc, v.CurrentProposedEndUtc, token)) return new CommandEffect(CommandStatus.Conflict);
                var now = clock.GetUtcNow().UtcDateTime;
                if (now >= v.PendingExpiresAtUtc) return new CommandEffect(CommandStatus.Conflict);
                ViewingEvent record;
                try { v.AcceptAlternative(request.Version, employee, now, context.ActorAccountId); record = ViewingEvent.AlternativeAccepted(context.CommandId, v); }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                catch (InvalidOperationException) { return new CommandEffect(CommandStatus.Conflict); }
                history.AddEvent(record);
                context.Emit("Viewing.Confirmed", 1, v.CustomerId, JsonSerializer.SerializeToElement(new { viewingId = v.Id, customerId = v.CustomerId,
                    unitId = v.UnitId, employeeId = employee, confirmedStartUtc = v.ConfirmedStartUtc, confirmedEndUtc = v.ConfirmedEndUtc, version = v.Version }));
                return new CommandEffect(CommandStatus.Success, new CommandOutcome("Viewing", v.Id, v.Version));
            }, ct);
        return Map(result);
    }
    private static ViewingResult<ViewingReply> Map(CommandResult result) => result.Status == CommandStatus.Success
        ? new(CommandStatus.Success, new(result.Outcome!.ResourceId, result.Outcome.ResourceVersion, result.IsReplay)) : new(result.Status);
}
