using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;
public sealed class ViewingRequestService(ICustomerRequestScope scope, IRequestUnitRepository units,
    ILeadRepository leads, ILeadOpportunityService opportunities, IViewingRepository repository,
    IViewingCustomerQueries queries, ICommandExecutor executor, ICurrentUser user, TimeProvider clock) : IViewingRequestService
{
    private Guid? Actor => user.IsAuthenticated && user.IsInRole(AppRoles.Customer) && user.AccountId is { } id && id != Guid.Empty ? id : null;
    private static bool Page(int page, int size) => page is > 0 and <= 1000000 && size is > 0 and <= 100;
    public Task<ViewingResult<ViewingPage<CustomerViewingDto>>> MineAsync(int page, int size, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new ViewingResult<ViewingPage<CustomerViewingDto>>(CommandStatus.Forbidden)) : !Page(page, size)
        ? Task.FromResult(new ViewingResult<ViewingPage<CustomerViewingDto>>(CommandStatus.Invalid)) : queries.MineAsync(actor, page, size, ct);
    public Task<ViewingResult<CustomerViewingDto>> GetAsync(Guid id, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new ViewingResult<CustomerViewingDto>(CommandStatus.Forbidden)) : queries.GetAsync(actor, id, ct);
    public Task<ViewingResult<ViewingPage<CustomerViewingEventDto>>> HistoryAsync(Guid id, int page, int size, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new ViewingResult<ViewingPage<CustomerViewingEventDto>>(CommandStatus.Forbidden)) : !Page(page, size)
        ? Task.FromResult(new ViewingResult<ViewingPage<CustomerViewingEventDto>>(CommandStatus.Invalid)) : queries.HistoryAsync(actor, id, page, size, ct);
    public Task<ViewingResult<CustomerViewingEventDto>> EventAsync(Guid id, Guid eventId, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new ViewingResult<CustomerViewingEventDto>(CommandStatus.Forbidden)) : queries.EventAsync(actor, id, eventId, ct);

    public async Task<ViewingResult<ViewingReply>> SubmitAsync(SubmitViewingDto request, string key, CancellationToken ct)
    {
        if (Actor is not { } actor) return new(CommandStatus.Forbidden);
        if (request.UnitId == Guid.Empty || request.ProposedStartUtc == default || request.ProposedStartUtc.Offset != TimeSpan.Zero)
            return new(CommandStatus.Invalid);
        Customer? customer = null;
        var result = await executor.ExecuteAsync("Viewing.Submit", key,
            JsonSerializer.SerializeToElement(new { unitId = request.UnitId, proposedStartUtc = request.ProposedStartUtc.UtcDateTime }),
            async token => { customer = null; var access = await scope.LockOwnedAsync(actor, token); customer = access.Customer; return access.Permission; },
            async (context, token) =>
            {
                var owner = customer ?? throw new InvalidOperationException("Locked owned Customer required.");
                var identity = await units.ResolveIdentityAsync(request.UnitId, token);
                if (identity is null) return new CommandEffect(CommandStatus.NotFound);
                // Match manual Lead range-before-catalog order, including an empty open-Lead range.
                await leads.LockOpenAsync(owner.Id, identity.ProjectId, token);
                var unit = await units.LockAsync(identity, token);
                if (unit is null || !unit.Published || !unit.DeveloperActive || !unit.ProjectActive) return new CommandEffect(CommandStatus.NotFound);
                TimeZoneInfo zone;
                try { zone = TimeZoneInfo.FindSystemTimeZoneById(unit.TimeZoneId); }
                catch (TimeZoneNotFoundException) { return new CommandEffect(CommandStatus.Conflict); }
                catch (InvalidTimeZoneException) { return new CommandEffect(CommandStatus.Conflict); }
                var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime);
                var agreements = await leads.LockAgreementsAsync(unit.DeveloperId, unit.ProjectId, localDate, token);
                if (agreements.Count == 0) return new CommandEffect(CommandStatus.NotFound);
                if (agreements.Count != 1) return new CommandEffect(CommandStatus.Conflict);
                var original = await opportunities.EnsureOpenAsync(owner.Id, unit.ProjectId, "ViewingRequest", context, token);
                if (original.Status != CommandStatus.Success) return new CommandEffect(original.Status);
                var now = clock.GetUtcNow();
                var finalDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
                if (finalDate != localDate)
                {
                    agreements = await leads.LockAgreementsAsync(unit.DeveloperId, unit.ProjectId, finalDate, token);
                    if (agreements.Count == 0) return new CommandEffect(CommandStatus.NotFound);
                    if (agreements.Count != 1) return new CommandEffect(CommandStatus.Conflict);
                }
                Viewing viewing;
                try { viewing = Viewing.Request(context.CommandId, owner.Id, original.Lead!.Id, unit.UnitId, unit.ProjectId,
                    agreements[0].Id, finalDate, unit.TimeZoneId, request.ProposedStartUtc.UtcDateTime, now.UtcDateTime, context.ActorAccountId); }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                repository.Add(viewing); repository.AddEvent(ViewingEvent.Requested(context.CommandId, viewing));
                context.Emit("Viewing.Requested", 1, owner.Id, JsonSerializer.SerializeToElement(new { viewingId = viewing.Id,
                    customerId = owner.Id, unitId = unit.UnitId, projectId = unit.ProjectId,
                    proposedStartUtc = viewing.CurrentProposedStartUtc, proposedEndUtc = viewing.CurrentProposedEndUtc, version = viewing.Version }));
                return new CommandEffect(CommandStatus.Success, new CommandOutcome("Viewing", viewing.Id, viewing.Version));
            }, ct);
        return result.Status == CommandStatus.Success
            ? new(CommandStatus.Success, new(result.Outcome!.ResourceId, result.Outcome.ResourceVersion, result.IsReplay)) : new(result.Status);
    }
}
