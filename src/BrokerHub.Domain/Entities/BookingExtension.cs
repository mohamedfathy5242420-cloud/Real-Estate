namespace BrokerHub.Domain.Entities;
public sealed class BookingExtension
{
    public const int SourceMaxLength = 200, ReferenceMaxLength = 500, ReasonMaxLength = 1000;
    private BookingExtension() { }
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid UnitId { get; private set; }
    public long RequestedVersion { get; private set; }
    public long BookingVersionAfter { get; private set; }
    public DateTime PreviousReservationExpiresAtUtc { get; private set; }
    public DateTime NewReservationExpiresAtUtc { get; private set; }
    public DateTime DeveloperExtendedAtUtc { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public Guid RecorderAccountId { get; private set; }
    public Guid? RecorderEmployeeId { get; private set; }
    public string Source { get; private set; } = "";
    public string Reference { get; private set; } = "";
    public string? Reason { get; private set; }
    public long Version { get; private set; } = 1;
    public static BookingExtension Record(Guid id, Booking booking, long requestedVersion, DateTime oldDeadline, DateTime occurred,
        DateTime now, Guid actor, Guid? employee, string source, string reference, string? reason)
    {
        if (id == Guid.Empty || actor == Guid.Empty || employee == Guid.Empty || requestedVersion < 2 || booking.Version-requestedVersion != 1 ||
            booking.Status != Booking.Confirmed || booking.LastExtensionId != id || booking.LastExtendedAtUtc != now ||
            oldDeadline.Kind != DateTimeKind.Utc || oldDeadline <= now || booking.ReservationExpiresAtUtc is not { } deadline || deadline <= oldDeadline ||
            occurred == default || occurred.Kind != DateTimeKind.Utc || now.Kind != DateTimeKind.Utc || occurred > now ||
            string.IsNullOrWhiteSpace(source) || source.Trim().Length > SourceMaxLength ||
            string.IsNullOrWhiteSpace(reference) || reference.Trim().Length > ReferenceMaxLength || reason?.Trim().Length > ReasonMaxLength)
            throw new ArgumentException("Truthful current external extension and recorder evidence required.");
        return new() { Id=id,BookingId=booking.Id,CustomerId=booking.CustomerId,UnitId=booking.UnitId,RequestedVersion=requestedVersion,
            BookingVersionAfter=booking.Version,PreviousReservationExpiresAtUtc=oldDeadline,NewReservationExpiresAtUtc=deadline,
            DeveloperExtendedAtUtc=occurred,RecordedAtUtc=now,RecorderAccountId=actor,RecorderEmployeeId=employee,
            Source=source.Trim(),Reference=reference.Trim(),Reason=string.IsNullOrWhiteSpace(reason)?null:reason.Trim() };
    }
}
