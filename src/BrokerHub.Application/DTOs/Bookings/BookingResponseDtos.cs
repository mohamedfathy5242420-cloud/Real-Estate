namespace BrokerHub.Application.DTOs.Bookings;
public sealed record RecordBookingResponseDto(long Version, string ResponseKind, DateTimeOffset DeveloperRespondedAtUtc,
    string Source, string Reference, decimal? OfferedPrice, string? CurrencyCode, DateTimeOffset? OfferedDeadlineUtc, string? Reason);
public sealed record StaffBookingResponseDto(Guid Id, Guid BookingId, Guid CustomerId, Guid UnitId, long RequestedVersion,
    long BookingVersionAfter, string ResponseKind, string Decision, string StatusAfter, DateTime DeveloperRespondedAtUtc,
    DateTime RecordedAtUtc, Guid RecorderAccountId, Guid? RecorderEmployeeId, string Source, string Reference,
    decimal? OfferedPrice, string? CurrencyCode, DateTime? OfferedDeadlineUtc, string? Reason, long Version);
