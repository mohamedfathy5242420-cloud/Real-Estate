using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.DTOs.Leads;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;
public sealed class LeadService(ILeadRepository repository, ILeadQueries queries, ICrmCustomerRepository scope,
    ILeadOpportunityService opportunities, ICommandExecutor executor, ICurrentUser user, TimeProvider clock) : ILeadService
{
    private Guid? Actor => user.IsAuthenticated && user.AccountId is { } id && id != Guid.Empty &&
        (user.IsInRole(AppRoles.SalesManager) || user.IsInRole(AppRoles.SalesEmployee)) ? id : null;
    private static bool Page(int page, int size) => page is > 0 and <= 1000000 && size is > 0 and <= 100;
    public Task<CrmResult<CrmPage<LeadDto>>> ListAsync(Guid customerId, int page, int size, string? status, CancellationToken ct) =>
        Actor is not { } actor ? Task.FromResult(new CrmResult<CrmPage<LeadDto>>(CrmStatus.Forbidden)) :
        !Page(page, size) || (status is not null && status is not (Lead.Open or Lead.Closed)) ? Task.FromResult(new CrmResult<CrmPage<LeadDto>>(CrmStatus.Invalid)) : queries.ListAsync(actor, customerId, page, size, status, ct);
    public Task<CrmResult<LeadDto>> GetAsync(Guid customerId, Guid id, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new CrmResult<LeadDto>(CrmStatus.Forbidden)) : queries.GetAsync(actor, customerId, id, ct);
    public Task<CrmResult<CrmPage<LeadEventDto>>> HistoryAsync(Guid customerId, Guid id, int page, int size, CancellationToken ct) =>
        Actor is not { } actor ? Task.FromResult(new CrmResult<CrmPage<LeadEventDto>>(CrmStatus.Forbidden)) :
        !Page(page, size) ? Task.FromResult(new CrmResult<CrmPage<LeadEventDto>>(CrmStatus.Invalid)) : queries.HistoryAsync(actor, customerId, id, page, size, ct);
    public Task<CrmResult<LeadEventDto>> EventAsync(Guid customerId, Guid id, Guid eventId, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new CrmResult<LeadEventDto>(CrmStatus.Forbidden)) : queries.EventAsync(actor, customerId, id, eventId, ct);
    public async Task<CrmResult<LeadReply>> OpenAsync(Guid customerId, OpenLeadDto request, string key, CancellationToken ct)
    {
        if (Actor is not { } actor) return new(CrmStatus.Forbidden);
        if (customerId == Guid.Empty || request.ProjectId == Guid.Empty || string.IsNullOrWhiteSpace(request.Source) || request.Source.Trim().Length > Lead.SourceMaxLength) return new(CrmStatus.Invalid);
        var intent = JsonSerializer.SerializeToElement(new { customerId, projectId = request.ProjectId, source = request.Source.Trim() });
        var result = await executor.ExecuteAsync("Crm.Lead.Open", key, intent,
            async token => (await scope.LockForSalesAsync(customerId, actor, false, token)).Permission,
            async (context, token) =>
            {
                var opened = await opportunities.EnsureOpenAsync(customerId, request.ProjectId, request.Source.Trim(), context, token);
                return opened.Status == CommandStatus.Success
                    ? new CommandEffect(CommandStatus.Success, new(opened.Created ? "LeadCreated" : "LeadReused", opened.Lead!.Id, opened.Lead.Version)) : new CommandEffect(opened.Status);
            }, ct);
        return Map(result, customerId);
    }
    public async Task<CrmResult<LeadReply>> CloseAsync(Guid customerId, Guid id, CloseLeadDto request, string key, CancellationToken ct)
    {
        if (Actor is not { } actor) return new(CrmStatus.Forbidden);
        if (customerId == Guid.Empty || id == Guid.Empty || request.Version <= 0 || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > Lead.ReasonMaxLength) return new(CrmStatus.Invalid);
        Lead? lead = null;
        var result = await executor.ExecuteAsync("Crm.Lead.Close", key, JsonSerializer.SerializeToElement(new { customerId, id, version = request.Version, reason = request.Reason.Trim() }),
            async token =>
            {
                lead = null; var access = await scope.LockForSalesAsync(customerId, actor, false, token);
                if (access.Permission != CommandPermission.Allowed) return access.Permission;
                lead = await repository.LockByIdAsync(customerId, id, token); return lead is null ? CommandPermission.NotFound : CommandPermission.Allowed;
            },
            (context, _) =>
            {
                var l = lead ?? throw new InvalidOperationException("Authorized Lead required.");
                if (l.Status != Lead.Open || l.Version != request.Version || l.Version == long.MaxValue) return Task.FromResult(new CommandEffect(CommandStatus.Conflict));
                var now = clock.GetUtcNow().UtcDateTime;
                try { l.Close(request.Version, request.Reason, now, context.ActorAccountId); }
                catch (ArgumentException) { return Task.FromResult(new CommandEffect(CommandStatus.Invalid)); }
                repository.AddEvent(LeadEvent.Record(context.CommandId, l, context.ActorAccountId, now));
                context.Emit("Crm.LeadClosed", 1, customerId, JsonSerializer.SerializeToElement(new { customerId, leadId = l.Id, leadVersion = l.Version }));
                return Task.FromResult(new CommandEffect(CommandStatus.Success, new CommandOutcome("LeadClosed", l.Id, l.Version)));
            }, ct);
        return Map(result, customerId);
    }
    private static CrmResult<LeadReply> Map(CommandResult result, Guid customerId) => result.Status switch
    {
        CommandStatus.Success => new(CrmStatus.Success, new(result.Outcome!.ResourceId, customerId, result.Outcome.ResourceVersion, result.Outcome.ResourceType == "LeadCreated", result.IsReplay)),
        CommandStatus.Forbidden => new(CrmStatus.Forbidden), CommandStatus.NotFound => new(CrmStatus.NotFound),
        CommandStatus.Conflict => new(CrmStatus.Conflict), _ => new(CrmStatus.Invalid)
    };
}
