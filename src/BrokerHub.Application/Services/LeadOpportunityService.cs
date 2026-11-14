using System.Text.Json;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Leads;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;
public sealed class LeadOpportunityService(ILeadRepository repository, TimeProvider clock) : ILeadOpportunityService
{
    public async Task<LeadOpenResult> EnsureOpenAsync(Guid customerId, Guid projectId, string source, CommandExecutionContext context, CancellationToken ct)
    {
        if (customerId == Guid.Empty || projectId == Guid.Empty || string.IsNullOrWhiteSpace(source) || source.Trim().Length > Lead.SourceMaxLength)
            return new(CommandStatus.Invalid);
        var existing = await repository.LockOpenAsync(customerId, projectId, ct);
        if (existing is not null) return new(CommandStatus.Success, existing);
        var project = await repository.LockProjectOriginAsync(projectId, ct);
        if (project is null || !project.DeveloperActive || !project.ProjectActive) return new(CommandStatus.Conflict);
        var now = clock.GetUtcNow(); DateOnly localDate;
        try { localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById(project.TimeZoneId)).DateTime); }
        catch (TimeZoneNotFoundException) { return new(CommandStatus.Conflict); }
        catch (InvalidTimeZoneException) { return new(CommandStatus.Conflict); }
        var origins = await repository.LockAgreementsAsync(project.DeveloperId, projectId, localDate, ct);
        if (origins.Count != 1) return new(CommandStatus.Conflict);
        var origin = origins[0]; Lead lead;
        try { lead = Lead.Create(Guid.NewGuid(), customerId, projectId, origin.Id, origin.Version, origin.StartsOn, origin.EndsOn,
            origin.ProtectionPeriodDays, localDate, project.TimeZoneId, now.UtcDateTime, context.ActorAccountId, source); }
        catch (ArgumentException) { return new(CommandStatus.Conflict); }
        repository.Add(lead); repository.AddEvent(LeadEvent.Record(context.CommandId, lead, context.ActorAccountId, now.UtcDateTime));
        context.Emit("Crm.LeadOpened", 1, customerId, JsonSerializer.SerializeToElement(new { customerId, leadId = lead.Id, projectId, marketingAgreementId = origin.Id, leadVersion = lead.Version }));
        return new(CommandStatus.Success, lead, true);
    }
}
