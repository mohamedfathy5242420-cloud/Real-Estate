namespace BrokerHub.Application.DTOs.Bookings;
public sealed record RecordBookingDepositDto(long Version, decimal Amount, string CurrencyCode, DateTimeOffset PaidAtUtc,
    string Source, string Reference, string? Notes = null);
public sealed record CustomerBookingDepositDto(Guid Id, Guid BookingId, decimal Amount, string CurrencyCode,
    DateTime PaidAtUtc, DateTime RecordedAtUtc, long Version, decimal RecordedRefundAmount, decimal RecordedUnrefundedAmount, string RecordedRefundStatus);
public sealed record StaffBookingDepositDto(Guid Id, Guid BookingId, Guid CustomerId, Guid UnitId, long RequestedVersion,
    decimal Amount, string CurrencyCode, DateTime PaidAtUtc, DateTime RecordedAtUtc, Guid RecorderAccountId,
    Guid? RecorderEmployeeId, string Source, string Reference, string? Notes, long Version,
    decimal RecordedRefundAmount, decimal RecordedUnrefundedAmount, string RecordedRefundStatus);
