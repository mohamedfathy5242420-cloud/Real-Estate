namespace BrokerHub.Domain.Entities;

public sealed class BookingRefund
{
    public const int SourceMaxLength = BookingDeposit.SourceMaxLength, ReferenceMaxLength = BookingDeposit.ReferenceMaxLength, NotesMaxLength = BookingDeposit.NotesMaxLength;
    private BookingRefund() { }
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid DepositId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid UnitId { get; private set; }
    public long RequestedVersion { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public DateTime DepositPaidAtUtc { get; private set; }
    public DateTime ReturnedAtUtc { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public Guid RecorderAccountId { get; private set; }
    public Guid? RecorderEmployeeId { get; private set; }
    public string Source { get; private set; } = "";
    public string Reference { get; private set; } = "";
    public string? Notes { get; private set; }
    public long Version { get; private set; } = 1;
    public static bool CanRecordAt(Booking b, BookingDeposit d, DateTime? latest, DateTime now) =>
        BookingDeposit.CanRecordAt(b,now) && now >= d.RecordedAtUtc && !(now < latest);
    public static BookingRefund Record(Guid id, Booking b, BookingDeposit d, long version, decimal alreadyReturned,
        DateTime? latest, decimal amount, string currency, DateTime returned, DateTime recorded, Guid actor,
        Guid? employee, string source, string reference, string? notes)
    {
        if (version <= 0 || b.Version != version || d.Version != 1 || d.BookingId != b.Id || d.CustomerId != b.CustomerId ||
            d.UnitId != b.UnitId || !CanRecordAt(b,d,latest,recorded) || alreadyReturned < 0 || alreadyReturned > d.Amount ||
            amount > d.Amount-alreadyReturned) throw new InvalidOperationException("Current scoped parent/version/recording audit and available recorded principal required.");
        if (id == Guid.Empty || actor == Guid.Empty || employee == Guid.Empty || !BookingDeposit.ValidMoney(amount,currency) ||
            currency != d.CurrencyCode || returned == default || returned.Kind != DateTimeKind.Utc || returned < d.PaidAtUtc ||
            returned > recorded || string.IsNullOrWhiteSpace(source) || source.Length > SourceMaxLength ||
            string.IsNullOrWhiteSpace(reference) || reference.Length > ReferenceMaxLength || notes?.Length > NotesMaxLength)
            throw new ArgumentException("Actual original-currency external return and bounded evidence/recorder required.");
        return new() { Id=id,BookingId=b.Id,DepositId=d.Id,CustomerId=b.CustomerId,UnitId=b.UnitId,RequestedVersion=version,
            Amount=amount,CurrencyCode=currency,DepositPaidAtUtc=DateTime.SpecifyKind(d.PaidAtUtc,DateTimeKind.Utc),
            ReturnedAtUtc=returned,RecordedAtUtc=recorded,RecorderAccountId=actor,RecorderEmployeeId=employee,
            Source=source.Trim(),Reference=reference.Trim(),Notes=string.IsNullOrWhiteSpace(notes)?null:notes.Trim() };
    }
}
