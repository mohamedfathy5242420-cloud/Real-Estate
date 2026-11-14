using BrokerHub.Application.DTOs.Commands;
namespace BrokerHub.Application.DTOs.Bookings;
public sealed record BookingResult<T>(CommandStatus Status, T? Value = default);
public sealed record BookingPage<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public sealed record SubmitBookingDto(Guid UnitId, string? RequestReason);
public sealed record BookingReply(Guid Id, long Version, bool IsReplay);
public sealed record CustomerBookingDto(Guid Id, Guid UnitId, Guid ProjectId, string Status, DateTime RequestedAtUtc,
    DateTime ResponseDueAtUtc, string? RequestReason, long Version, string? RejectionReason,
    DateTime? ConfirmedAtUtc, decimal? ConfirmedPrice, string? ConfirmedCurrencyCode, DateTime? ReservationExpiresAtUtc,
    DateTime? CancelledAtUtc, string? CancellationReason,DateTime? CompletedAtUtc=null);
public sealed record CustomerBookingEventDto(Guid Id, Guid BookingId, long BookingVersion, string Kind, DateTime AtUtc, string Status, string? ReasonCode,
    decimal? ConfirmedPrice, string? ConfirmedCurrencyCode, DateTime? ReservationExpiresAtUtc, string? CancelledFromStatus, string? CancellationReason);
public sealed record StaffBookingDto(Guid Id, Guid CustomerId, Guid LeadId, Guid UnitId, Guid ProjectId, string Status,
    DateTime RequestedAtUtc, Guid RequestedByAccountId, DateTime ResponseDueAtUtc, Guid SubmissionAgreementId,
    DateOnly SubmissionLocalDate, string TimeZoneId, string? RequestReason, long Version, DateTime? RejectedAtUtc,
    string? RejectionReason, Guid? RejectedByAccountId, string? RejectionActorKind, DateTime? ConfirmedAtUtc,
    Guid? ConfirmedByAccountId, decimal? ConfirmedPrice, string? ConfirmedCurrencyCode, DateTime? ReservationExpiresAtUtc,
    Guid? ConfirmedResponseId, DateTime? ExpiredAtUtc, DateTime? LastExtendedAtUtc, Guid? LastExtensionId,
    DateTime? CancelledAtUtc, Guid? CancelledByAccountId, string? CancellationReason, string? CancelledFromStatus,
    DateTime? CompletedAtUtc=null,Guid? CompletedByAccountId=null,Guid? CompletedDealId=null);
public sealed record StaffBookingEventDto(Guid Id, Guid BookingId, Guid CustomerId, long BookingVersion, string Kind,
    string ActorKind, Guid? ActorAccountId, DateTime AtUtc, string Status, string? ReasonCode, DateTime? EffectiveCutoffUtc,
    decimal? ConfirmedPrice, string? ConfirmedCurrencyCode, DateTime? ReservationExpiresAtUtc, Guid? DeveloperResponseId,
    DateTime? PreviousReservationExpiresAtUtc, Guid? ExtensionId, string? CancelledFromStatus, string? CancellationReason,Guid? DealId=null);
