using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;
public static class ViewingCoordinationMappings
{
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    private static DateTime? Utc(DateTime? value) => value is { } date ? Utc(date) : null;
    public static StaffViewingDto ToStaffDto(this Viewing v, DateTime observedUtc) => new(v.Id, v.CustomerId, v.LeadId, v.UnitId, v.ProjectId,
        v.EffectiveStatus(observedUtc), Utc(v.CurrentProposedStartUtc), Utc(v.CurrentProposedEndUtc), v.TimeZoneId, Utc(v.RequestedAtUtc),
        Utc(v.PendingCycleStartedAtUtc), Utc(v.PendingExpiresAtUtc), v.RequestedByAccountId, Utc(v.ConfirmedStartUtc),
        Utc(v.ConfirmedEndUtc), v.ConfirmedEmployeeId, Utc(v.ConfirmedAtUtc), v.ConfirmedByAccountId, Utc(v.CoordinatedAtUtc),
        v.CoordinationSource, v.CoordinationReference, v.Version, Utc(v.OfferedAtUtc), v.OfferedByAccountId, v.OfferedEmployeeId,
        Utc(v.CancelledAtUtc), v.CancelledByAccountId, v.CancellationKind, v.CancellationReason, v.CancelledFromStatus,
        Utc(v.ExpiredAtUtc), v.ExpiredFromStatus, Utc(v.OutcomeOccurredAtUtc), Utc(v.OutcomeRecordedAtUtc),
        v.OutcomeRecordedByAccountId, v.OutcomeNoteId);
    public static StaffViewingEventDto ToStaffDto(this ViewingEvent e) => new(e.Id, e.ViewingId, e.CustomerId,
        e.ViewingVersion, e.Kind, e.ActorAccountId, Utc(e.AtUtc), Utc(e.ProposedStartUtc), Utc(e.ProposedEndUtc),
        Utc(e.ConfirmedStartUtc), Utc(e.ConfirmedEndUtc), e.ConfirmedEmployeeId, Utc(e.CoordinatedAtUtc), e.CoordinationSource, e.CoordinationReference,
        e.OfferedEmployeeId, Utc(e.PreviousProposedStartUtc), Utc(e.PreviousProposedEndUtc), e.Reason, e.PreviousStatus,
        Utc(e.PreviousConfirmedStartUtc), Utc(e.PreviousConfirmedEndUtc), e.PreviousConfirmedEmployeeId, e.CancellationKind,
        e.ActorKind, Utc(e.EffectiveCutoffUtc), Utc(e.OutcomeOccurredAtUtc), e.OutcomeNoteId);
}
