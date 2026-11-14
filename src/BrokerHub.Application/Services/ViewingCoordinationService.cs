using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;
public sealed class ViewingCoordinationService(ICrmCustomerRepository scope, IViewingCoordinationRepository repository,
    IViewingRepository history, IViewingStaffQueries queries, ICommandExecutor executor, ICurrentUser user, TimeProvider clock) : IViewingCoordinationService
{
    private Guid? Actor => user.IsAuthenticated && (user.IsInRole(AppRoles.SalesManager) || user.IsInRole(AppRoles.SalesEmployee)) &&
        user.AccountId is { } id && id != Guid.Empty ? id : null;
    private static bool Page(int page, int size) => page is > 0 and <= 1000000 && size is > 0 and <= 100;
    public Task<ViewingResult<ViewingPage<StaffViewingDto>>> ListAsync(Guid? customerId, int page, int size, string? status, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new ViewingResult<ViewingPage<StaffViewingDto>>(CommandStatus.Forbidden)) : !Page(page, size) || (status is not null && status is not (Viewing.PendingConfirmation or Viewing.Confirmed or Viewing.AwaitingCustomerAcceptance or Viewing.Cancelled or Viewing.Expired or Viewing.Completed or Viewing.CustomerNoShow))
        ? Task.FromResult(new ViewingResult<ViewingPage<StaffViewingDto>>(CommandStatus.Invalid)) : queries.ListAsync(actor, customerId, page, size, status, ct);
    public Task<ViewingResult<StaffViewingDto>> GetAsync(Guid customerId, Guid id, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new ViewingResult<StaffViewingDto>(CommandStatus.Forbidden)) : queries.GetAsync(actor, customerId, id, ct);
    public Task<ViewingResult<ViewingPage<StaffViewingEventDto>>> HistoryAsync(Guid customerId, Guid id, int page, int size, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new ViewingResult<ViewingPage<StaffViewingEventDto>>(CommandStatus.Forbidden)) : !Page(page, size)
        ? Task.FromResult(new ViewingResult<ViewingPage<StaffViewingEventDto>>(CommandStatus.Invalid)) : queries.HistoryAsync(actor, customerId, id, page, size, ct);
    public Task<ViewingResult<StaffViewingEventDto>> EventAsync(Guid customerId, Guid id, Guid eventId, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new ViewingResult<StaffViewingEventDto>(CommandStatus.Forbidden)) : queries.EventAsync(actor, customerId, id, eventId, ct);
    public async Task<ViewingResult<ViewingReply>> ConfirmAsync(Guid customerId, Guid id, ConfirmViewingDto request, string key, CancellationToken ct)
    {
        if (Actor is not { } actor) return new(CommandStatus.Forbidden);
        if (customerId == Guid.Empty || id == Guid.Empty || request.Version <= 0 || request.ConfirmedStartUtc == default ||
            request.ConfirmedEndUtc <= request.ConfirmedStartUtc || request.CoordinatedAtUtc == default ||
            request.ConfirmedStartUtc.Offset != TimeSpan.Zero || request.ConfirmedEndUtc.Offset != TimeSpan.Zero || request.CoordinatedAtUtc.Offset != TimeSpan.Zero ||
            string.IsNullOrWhiteSpace(request.CoordinationSource) || request.CoordinationSource.Trim().Length > Viewing.CoordinationSourceMaxLength ||
            request.CoordinationReference?.Trim().Length > Viewing.CoordinationReferenceMaxLength) return new(CommandStatus.Invalid);
        Customer? customer = null; Viewing? viewing = null;
        var result = await executor.ExecuteAsync("Viewing.Confirm", key, JsonSerializer.SerializeToElement(new { customerId, id,
            version = request.Version, confirmedStartUtc = request.ConfirmedStartUtc.UtcDateTime, confirmedEndUtc = request.ConfirmedEndUtc.UtcDateTime,
            coordinatedAtUtc = request.CoordinatedAtUtc.UtcDateTime, coordinationSource = request.CoordinationSource.Trim(),
            coordinationReference = string.IsNullOrWhiteSpace(request.CoordinationReference) ? null : request.CoordinationReference.Trim() }),
            async token =>
            {
                customer = null; viewing = null; var access = await scope.LockForSalesAsync(customerId, actor, false, token);
                if (access.Permission != CommandPermission.Allowed) return access.Permission;
                customer = access.Customer; viewing = await repository.LockAsync(customerId, id, token);
                return viewing is null ? CommandPermission.NotFound : CommandPermission.Allowed;
            },
            async (context, token) =>
            {
                var v = viewing ?? throw new InvalidOperationException("Authorized Viewing required.");
                if (v.Status != Viewing.PendingConfirmation || v.Version != request.Version || v.Version == long.MaxValue || clock.GetUtcNow().UtcDateTime >= v.PendingExpiresAtUtc)
                    return new CommandEffect(CommandStatus.Conflict);
                if (v.CurrentProposedStartUtc != request.ConfirmedStartUtc.UtcDateTime) return new CommandEffect(CommandStatus.Invalid);
                if (customer!.AssignedEmployeeId is not { } employee) return new CommandEffect(CommandStatus.Conflict);
                var account = await repository.LockResponsibleAccountAsync(employee, token);
                if (account is null || !await repository.LockResourcesAsync(v.UnitId, employee, account.Value, token)) return new CommandEffect(CommandStatus.Conflict);
                if (await repository.ConflictsAsync(v.UnitId, employee, request.ConfirmedStartUtc.UtcDateTime, request.ConfirmedEndUtc.UtcDateTime, token))
                    return new CommandEffect(CommandStatus.Conflict);
                var now = clock.GetUtcNow().UtcDateTime;
                if (now >= v.PendingExpiresAtUtc) return new CommandEffect(CommandStatus.Conflict);
                try { v.Confirm(request.Version, employee, request.ConfirmedStartUtc.UtcDateTime, request.ConfirmedEndUtc.UtcDateTime,
                    request.CoordinatedAtUtc.UtcDateTime, request.CoordinationSource, request.CoordinationReference, now, context.ActorAccountId); }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                catch (InvalidOperationException) { return new CommandEffect(CommandStatus.Conflict); }
                history.AddEvent(ViewingEvent.Confirmation(context.CommandId, v));
                context.Emit("Viewing.Confirmed", 1, customerId, JsonSerializer.SerializeToElement(new { viewingId = v.Id, customerId,
                    unitId = v.UnitId, employeeId = employee, confirmedStartUtc = v.ConfirmedStartUtc, confirmedEndUtc = v.ConfirmedEndUtc, version = v.Version }));
                return new CommandEffect(CommandStatus.Success, new CommandOutcome("Viewing", v.Id, v.Version));
            }, ct);
        return result.Status == CommandStatus.Success ? new(CommandStatus.Success, new(result.Outcome!.ResourceId, result.Outcome.ResourceVersion, result.IsReplay)) : new(result.Status);
    }
}
