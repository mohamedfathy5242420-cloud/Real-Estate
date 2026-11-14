using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;
public sealed class ViewingOutcomeService(ICrmCustomerRepository scope, IViewingCoordinationRepository repository,
    IViewingLifecycleRepository resources, IViewingRepository history, ICrmFollowUpRepository notes,
    ICommandExecutor executor, ICurrentUser user, TimeProvider clock) : IViewingOutcomeService
{
    public async Task<ViewingResult<ViewingReply>> RecordAsync(Guid customerId, Guid id, RecordViewingOutcomeDto request, string key, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.AccountId is not { } actor || actor == Guid.Empty ||
            !(user.IsInRole(AppRoles.SalesEmployee) || user.IsInRole(AppRoles.SalesManager))) return new(CommandStatus.Forbidden);
        if (customerId == Guid.Empty || id == Guid.Empty || request.Version <= 0 ||
            request.Outcome is not (Viewing.Completed or Viewing.CustomerNoShow) || request.OccurredAtUtc == default ||
            request.OccurredAtUtc.Offset != TimeSpan.Zero || string.IsNullOrWhiteSpace(request.Notes) || request.Notes.Trim().Length > SalesNote.BodyMaxLength)
            return new(CommandStatus.Invalid);
        Viewing? viewing = null;
        var result = await executor.ExecuteAsync("Viewing.RecordOutcome", key, JsonSerializer.SerializeToElement(new {
            customerId, id, version = request.Version, outcome = request.Outcome,
            occurredAtUtc = request.OccurredAtUtc.UtcDateTime, notes = request.Notes.Trim() }),
            async token =>
            {
                viewing = null;
                var access = await scope.LockForSalesAsync(customerId, actor, false, token);
                if (access.Permission != CommandPermission.Allowed) return access.Permission;
                viewing = await repository.LockAsync(customerId, id, token);
                return viewing is null ? CommandPermission.NotFound : CommandPermission.Allowed;
            },
            async (context, token) =>
            {
                var v = viewing ?? throw new InvalidOperationException("Current authorized Viewing required.");
                if (v.Status != Viewing.Confirmed || v.Version != request.Version || v.Version == long.MaxValue ||
                    v.ConfirmedEmployeeId is not { } originalEmployee) return new CommandEffect(CommandStatus.Conflict);
                if (!await resources.LockReleasedResourcesAsync(v.UnitId, originalEmployee, token)) return new CommandEffect(CommandStatus.Conflict);
                var authorEmployee = await notes.FindAuthorEmployeeAsync(context.ActorAccountId, token);
                var now = clock.GetUtcNow().UtcDateTime;
                try { v.RecordOutcome(request.Version, request.Outcome, request.OccurredAtUtc.UtcDateTime, now, context.ActorAccountId, context.CommandId); }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                catch (InvalidOperationException) { return new CommandEffect(CommandStatus.Conflict); }
                notes.AddNote(SalesNote.Create(context.CommandId, v.CustomerId, v.LeadId, context.ActorAccountId, authorEmployee, request.Notes, now, null));
                history.AddEvent(ViewingEvent.Outcome(context.CommandId, v));
                context.Emit("Viewing.OutcomeRecorded", 1, v.CustomerId, JsonSerializer.SerializeToElement(new {
                    viewingId = v.Id, customerId = v.CustomerId, unitId = v.UnitId, confirmedEmployeeId = v.ConfirmedEmployeeId,
                    status = v.Status, occurredAtUtc = v.OutcomeOccurredAtUtc, recordedAtUtc = v.OutcomeRecordedAtUtc, version = v.Version }));
                return new CommandEffect(CommandStatus.Success, new CommandOutcome("Viewing", v.Id, v.Version));
            }, ct);
        return result.Status == CommandStatus.Success ? new(CommandStatus.Success, new(result.Outcome!.ResourceId, result.Outcome.ResourceVersion, result.IsReplay)) : new(result.Status);
    }
}
