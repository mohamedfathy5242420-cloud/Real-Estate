using BrokerHub.Application.DTOs.Leads;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;
public static class LeadMappings
{
    public static LeadDto ToDto(this Lead l) => new(l.Id, l.CustomerId, l.ProjectId, l.MarketingAgreementId,
        l.AgreementVersionAtOpening, l.AgreementStartsOn, l.AgreementEndsOn, l.ProtectionPeriodDays, l.ProtectionEndsOn,
        l.OpenedOnLocalDate, l.OpeningTimeZoneId, DateTime.SpecifyKind(l.OpenedAtUtc, DateTimeKind.Utc), l.OpenedByAccountId, l.Source, l.Status, l.Version,
        l.ClosedAtUtc is { } closed ? DateTime.SpecifyKind(closed, DateTimeKind.Utc) : null, l.ClosedByAccountId, l.CloseReason);
    public static LeadEventDto ToDto(this LeadEvent e) => new(e.Id, e.LeadId, e.CustomerId, e.LeadVersion, e.EventType, e.ActorAccountId, DateTime.SpecifyKind(e.AtUtc, DateTimeKind.Utc), e.Reason);
}
