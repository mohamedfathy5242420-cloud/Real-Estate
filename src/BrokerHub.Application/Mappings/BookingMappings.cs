using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Application.Mappings;
public static class BookingMappings
{
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    private static DateTime? Utc(DateTime? value) => value is { } date ? Utc(date) : null;
    public static CustomerBookingDto ToCustomerDto(this Booking b, DateTime observed) => new(b.Id, b.UnitId, b.ProjectId,
        b.EffectiveStatus(observed), Utc(b.RequestedAtUtc), Utc(b.ResponseDueAtUtc), b.RequestReason, b.Version, b.EffectiveRejectionReason(observed),
        Utc(b.ConfirmedAtUtc),b.ConfirmedPrice,b.ConfirmedCurrencyCode,Utc(b.ReservationExpiresAtUtc),Utc(b.CancelledAtUtc),b.CancellationReason,Utc(b.CompletedAtUtc));
    public static StaffBookingDto ToStaffDto(this Booking b, DateTime observed) => new(b.Id, b.CustomerId, b.LeadId, b.UnitId,
        b.ProjectId, b.EffectiveStatus(observed), Utc(b.RequestedAtUtc), b.RequestedByAccountId, Utc(b.ResponseDueAtUtc),
        b.SubmissionAgreementId, b.SubmissionLocalDate, b.TimeZoneId, b.RequestReason, b.Version, Utc(b.RejectedAtUtc),
        b.EffectiveRejectionReason(observed), b.RejectedByAccountId, b.RejectionActorKind,Utc(b.ConfirmedAtUtc),b.ConfirmedByAccountId,
        b.ConfirmedPrice,b.ConfirmedCurrencyCode,Utc(b.ReservationExpiresAtUtc),b.ConfirmedResponseId,Utc(b.ExpiredAtUtc),Utc(b.LastExtendedAtUtc),b.LastExtensionId,
        Utc(b.CancelledAtUtc),b.CancelledByAccountId,b.CancellationReason,b.CancelledFromStatus,Utc(b.CompletedAtUtc),b.CompletedByAccountId,b.CompletedDealId);
    public static CustomerBookingEventDto ToCustomerDto(this BookingEvent h) => new(h.Id, h.BookingId, h.BookingVersion,
        h.Kind, Utc(h.AtUtc), h.Status, h.ReasonCode,h.ConfirmedPrice,h.ConfirmedCurrencyCode,Utc(h.ReservationExpiresAtUtc),h.CancelledFromStatus,h.CancellationReason);
    public static StaffBookingEventDto ToStaffDto(this BookingEvent h) => new(h.Id, h.BookingId, h.CustomerId, h.BookingVersion,
        h.Kind, h.ActorKind, h.ActorAccountId, Utc(h.AtUtc), h.Status, h.ReasonCode, Utc(h.EffectiveCutoffUtc),
        h.ConfirmedPrice,h.ConfirmedCurrencyCode,Utc(h.ReservationExpiresAtUtc),h.DeveloperResponseId,Utc(h.PreviousReservationExpiresAtUtc),h.ExtensionId,
        h.CancelledFromStatus,h.CancellationReason,h.DealId);
    public static StaffBookingResponseDto ToStaffDto(this BookingDeveloperResponse r) => new(r.Id,r.BookingId,r.CustomerId,r.UnitId,
        r.RequestedVersion,r.BookingVersionAfter,r.ResponseKind,r.Decision,r.StatusAfter,Utc(r.DeveloperRespondedAtUtc),Utc(r.RecordedAtUtc),
        r.RecorderAccountId,r.RecorderEmployeeId,r.Source,r.Reference,r.OfferedPrice,r.CurrencyCode,Utc(r.OfferedDeadlineUtc),r.Reason,r.Version);
    public static StaffBookingExtensionDto ToStaffDto(this BookingExtension e) => new(e.Id,e.BookingId,e.CustomerId,e.UnitId,e.RequestedVersion,e.BookingVersionAfter,
        Utc(e.PreviousReservationExpiresAtUtc),Utc(e.NewReservationExpiresAtUtc),Utc(e.DeveloperExtendedAtUtc),Utc(e.RecordedAtUtc),e.RecorderAccountId,e.RecorderEmployeeId,
        e.Source,e.Reference,e.Reason,e.Version);
    private static decimal Unrefunded(BookingDeposit d, decimal returned) => returned >= 0 && returned <= d.Amount ? d.Amount-returned
        : throw new InvalidOperationException("Recorded refund ledger exceeds its original principal.");
    private static string RefundStatus(BookingDeposit d, decimal returned) => Unrefunded(d,returned) == 0 ? "Full" : returned == 0 ? "None" : "Partial";
    public static CustomerBookingDepositDto ToCustomerDto(this BookingDeposit d, decimal returned) => new(d.Id,d.BookingId,d.Amount,d.CurrencyCode,Utc(d.PaidAtUtc),Utc(d.RecordedAtUtc),d.Version,
        returned,Unrefunded(d,returned),RefundStatus(d,returned));
    public static StaffBookingDepositDto ToStaffDto(this BookingDeposit d, decimal returned) => new(d.Id,d.BookingId,d.CustomerId,d.UnitId,d.RequestedVersion,d.Amount,d.CurrencyCode,
        Utc(d.PaidAtUtc),Utc(d.RecordedAtUtc),d.RecorderAccountId,d.RecorderEmployeeId,d.Source,d.Reference,d.Notes,d.Version,
        returned,Unrefunded(d,returned),RefundStatus(d,returned));
    public static CustomerBookingRefundDto ToCustomerDto(this BookingRefund r) => new(r.Id,r.BookingId,r.DepositId,r.Amount,r.CurrencyCode,Utc(r.ReturnedAtUtc),Utc(r.RecordedAtUtc),r.Version);
    public static StaffBookingRefundDto ToStaffDto(this BookingRefund r) => new(r.Id,r.BookingId,r.DepositId,r.CustomerId,r.UnitId,r.RequestedVersion,r.Amount,r.CurrencyCode,
        Utc(r.DepositPaidAtUtc),Utc(r.ReturnedAtUtc),Utc(r.RecordedAtUtc),r.RecorderAccountId,r.RecorderEmployeeId,r.Source,r.Reference,r.Notes,r.Version);
}
