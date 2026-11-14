namespace BrokerHub.Application.DTOs.Bookings;
public sealed record RecordBookingRefundDto(long Version, decimal Amount, string CurrencyCode, DateTimeOffset ReturnedAtUtc,
    string Source, string Reference, string? Notes = null);
public sealed record BookingRefundLedger(decimal RecordedAmount, DateTime? LastRecordedAtUtc);
public sealed record CustomerBookingRefundDto(Guid Id, Guid BookingId, Guid DepositId, decimal Amount, string CurrencyCode,
    DateTime ReturnedAtUtc, DateTime RecordedAtUtc, long Version);
public sealed record StaffBookingRefundDto(Guid Id, Guid BookingId, Guid DepositId, Guid CustomerId, Guid UnitId, long RequestedVersion,
    decimal Amount, string CurrencyCode, DateTime DepositPaidAtUtc, DateTime ReturnedAtUtc, DateTime RecordedAtUtc,
    Guid RecorderAccountId, Guid? RecorderEmployeeId, string Source, string Reference, string? Notes, long Version);
