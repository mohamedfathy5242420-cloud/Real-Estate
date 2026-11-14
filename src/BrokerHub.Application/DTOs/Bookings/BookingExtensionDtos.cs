namespace BrokerHub.Application.DTOs.Bookings;
public sealed record RecordBookingExtensionDto(long Version, DateTimeOffset DeveloperExtendedAtUtc, DateTimeOffset NewReservationExpiresAtUtc,
    string Source, string Reference, string? Reason);
public sealed record StaffBookingExtensionDto(Guid Id, Guid BookingId, Guid CustomerId, Guid UnitId, long RequestedVersion,
    long BookingVersionAfter, DateTime PreviousReservationExpiresAtUtc, DateTime NewReservationExpiresAtUtc, DateTime DeveloperExtendedAtUtc,
    DateTime RecordedAtUtc, Guid RecorderAccountId, Guid? RecorderEmployeeId, string Source, string Reference, string? Reason, long Version);
