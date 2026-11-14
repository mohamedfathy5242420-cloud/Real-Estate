namespace BrokerHub.Application.DTOs.Viewings;
public sealed record ConfirmViewingDto(long Version, DateTimeOffset ConfirmedStartUtc, DateTimeOffset ConfirmedEndUtc,
    DateTimeOffset CoordinatedAtUtc, string CoordinationSource, string? CoordinationReference);
public sealed record StaffViewingDto(Guid Id, Guid CustomerId, Guid LeadId, Guid UnitId, Guid ProjectId, string Status,
    DateTime CurrentProposedStartUtc, DateTime CurrentProposedEndUtc, string TimeZoneId, DateTime RequestedAtUtc,
    DateTime PendingCycleStartedAtUtc, DateTime PendingExpiresAtUtc, Guid RequestedByAccountId,
    DateTime? ConfirmedStartUtc, DateTime? ConfirmedEndUtc, Guid? ConfirmedEmployeeId, DateTime? ConfirmedAtUtc,
    Guid? ConfirmedByAccountId, DateTime? CoordinatedAtUtc, string? CoordinationSource, string? CoordinationReference, long Version,
    DateTime? OfferedAtUtc, Guid? OfferedByAccountId, Guid? OfferedEmployeeId, DateTime? CancelledAtUtc,
    Guid? CancelledByAccountId, string? CancellationKind, string? CancellationReason, string? CancelledFromStatus,
    DateTime? ExpiredAtUtc, string? ExpiredFromStatus, DateTime? OutcomeOccurredAtUtc,
    DateTime? OutcomeRecordedAtUtc, Guid? OutcomeRecordedByAccountId, Guid? OutcomeNoteId);
public sealed record StaffViewingEventDto(Guid Id, Guid ViewingId, Guid CustomerId, long ViewingVersion, string Kind,
    Guid? ActorAccountId, DateTime AtUtc, DateTime ProposedStartUtc, DateTime ProposedEndUtc, DateTime? ConfirmedStartUtc,
    DateTime? ConfirmedEndUtc, Guid? ConfirmedEmployeeId, DateTime? CoordinatedAtUtc, string? CoordinationSource, string? CoordinationReference,
    Guid? OfferedEmployeeId, DateTime? PreviousProposedStartUtc, DateTime? PreviousProposedEndUtc, string? Reason,
    string? PreviousStatus, DateTime? PreviousConfirmedStartUtc, DateTime? PreviousConfirmedEndUtc,
    Guid? PreviousConfirmedEmployeeId, string? CancellationKind, string ActorKind, DateTime? EffectiveCutoffUtc,
    DateTime? OutcomeOccurredAtUtc, Guid? OutcomeNoteId);
