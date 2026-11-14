using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;
public static class ViewingMappings
{
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    private static DateTime? Utc(DateTime? value) => value is { } date ? Utc(date) : null;
    public static CustomerViewingDto ToCustomerDto(this Viewing v, DateTime observedUtc) => new(v.Id, v.UnitId, v.ProjectId, v.EffectiveStatus(observedUtc),
        Utc(v.CurrentProposedStartUtc), Utc(v.CurrentProposedEndUtc), v.TimeZoneId, Utc(v.RequestedAtUtc),
        Utc(v.PendingCycleStartedAtUtc), Utc(v.PendingExpiresAtUtc), v.Version, Utc(v.ConfirmedStartUtc), Utc(v.ConfirmedEndUtc));
    public static CustomerViewingEventDto ToCustomerDto(this ViewingEvent e) => new(e.Id, e.ViewingId,
        e.ViewingVersion, e.Kind, Utc(e.AtUtc), Utc(e.ProposedStartUtc), Utc(e.ProposedEndUtc), Utc(e.ConfirmedStartUtc), Utc(e.ConfirmedEndUtc));
}
