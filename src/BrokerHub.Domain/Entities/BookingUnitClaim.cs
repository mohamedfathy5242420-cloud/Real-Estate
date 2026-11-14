namespace BrokerHub.Domain.Entities;

// Operational current pointer; immutable booking facts/history remain on their original roots.
public sealed class BookingUnitClaim
{
    private BookingUnitClaim() { }
    public Guid UnitId { get; private set; }
    public Guid? BookingId { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public long Version { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public bool IsCurrent(DateTime now) => BookingId.HasValue && ExpiresAtUtc > now;
    public static BookingUnitClaim Create(Guid unit, Guid booking, DateTime deadline, DateTime now)
    {
        if (unit == Guid.Empty) throw new ArgumentException("Unit required.");
        var claim = new BookingUnitClaim { UnitId = unit, Version = 0 };
        claim.Assign(booking,deadline,now); return claim;
    }
    public void Assign(Guid booking, DateTime deadline, DateTime now)
    {
        ValidateTime(now);
        if (IsCurrent(now) || booking == Guid.Empty || deadline.Kind != DateTimeKind.Utc || deadline <= now)
            throw new InvalidOperationException("Unit has a current claim or the new reservation has expired.");
        BookingId = booking; ExpiresAtUtc = deadline; UpdatedAtUtc = now; Version++;
    }
    public bool Release(Guid booking, DateTime now)
    {
        if (BookingId != booking) return false;
        ValidateTime(now); BookingId = null; ExpiresAtUtc = null; UpdatedAtUtc = now; Version++; return true;
    }
    public void Extend(Guid booking, DateTime oldDeadline, DateTime newDeadline, DateTime now)
    {
        ValidateTime(now);
        if (BookingId != booking || !IsCurrent(now) || ExpiresAtUtc != oldDeadline || newDeadline.Kind != DateTimeKind.Utc || newDeadline <= oldDeadline)
            throw new InvalidOperationException("Same current booking/claim and a later external deadline required.");
        ExpiresAtUtc = newDeadline; UpdatedAtUtc = now; Version++;
    }
    private void ValidateTime(DateTime now)
    {
        if (Version == long.MaxValue || now == default || now.Kind != DateTimeKind.Utc || now < UpdatedAtUtc)
            throw new InvalidOperationException("Current claim version and UTC required.");
    }
}
