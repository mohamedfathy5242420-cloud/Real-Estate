namespace BrokerHub.Domain.Entities;

public sealed class BookingDeposit
{
    public const int SourceMaxLength = 200, ReferenceMaxLength = 500, NotesMaxLength = 1000;
    public const decimal MaxAmount = 9999999999999999.99m;
    private BookingDeposit() { }
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid UnitId { get; private set; }
    public long RequestedVersion { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public DateTime PaidAtUtc { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public Guid RecorderAccountId { get; private set; }
    public Guid? RecorderEmployeeId { get; private set; }
    public string Source { get; private set; } = "";
    public string Reference { get; private set; } = "";
    public string? Notes { get; private set; }
    public long Version { get; private set; } = 1;
    public static bool ValidMoney(decimal amount, string currency) => amount is > 0 and <= MaxAmount &&
        decimal.Truncate(amount * 100) == amount * 100 && currency.Length == 3 && currency.All(c => c is >= 'A' and <= 'Z');
    public static bool CanRecordAt(Booking booking, DateTime now) => now != default && now.Kind == DateTimeKind.Utc &&
        now >= booking.RequestedAtUtc && !(now < booking.ConfirmedAtUtc || now < booking.LastExtendedAtUtc ||
        now < booking.CancelledAtUtc || now < booking.ExpiredAtUtc || now < booking.RejectedAtUtc || now < booking.CompletedAtUtc);
    public static BookingDeposit Record(Guid id, Booking booking, long version, decimal amount, string currency,
        DateTime paid, DateTime recorded, Guid actor, Guid? employee, string source, string reference, string? notes)
    {
        if (version <= 0 || booking.Version != version || !CanRecordAt(booking,recorded))
            throw new InvalidOperationException("Current booking version and actual recording chronology required.");
        if (id == Guid.Empty || actor == Guid.Empty || employee == Guid.Empty || !ValidMoney(amount,currency) ||
            paid == default || paid.Kind != DateTimeKind.Utc || paid > recorded || string.IsNullOrWhiteSpace(source) ||
            source.Length > SourceMaxLength || string.IsNullOrWhiteSpace(reference) || reference.Length > ReferenceMaxLength ||
            notes?.Length > NotesMaxLength) throw new ArgumentException("Actual bounded external payment and recorder required.");
        return new() { Id=id,BookingId=booking.Id,CustomerId=booking.CustomerId,UnitId=booking.UnitId,
            RequestedVersion=version,Amount=amount,CurrencyCode=currency,PaidAtUtc=paid,RecordedAtUtc=recorded,
            RecorderAccountId=actor,RecorderEmployeeId=employee,Source=source.Trim(),Reference=reference.Trim(),
            Notes=string.IsNullOrWhiteSpace(notes)?null:notes.Trim() };
    }
}
