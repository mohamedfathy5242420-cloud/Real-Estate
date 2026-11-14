namespace BrokerHub.Domain.Entities;

public sealed class BookingDeveloperResponse
{
    public const decimal MaxPrice = 9999999999999999.99m;
    public const int SourceMaxLength = 200, ReferenceMaxLength = 500, ReasonMaxLength = 1000;
    public const string ConfirmedDecision = "Confirmed", DeveloperRejected = "DeveloperRejected", InvalidDeadline = "InvalidDeadline",
        LocalConflict = "LocalConflict", ResponseTimeoutDecision = "ResponseTimeout", ReservationExpiredDecision = "ReservationExpired", StateUnchanged = "StateUnchanged";
    private BookingDeveloperResponse() { }
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid UnitId { get; private set; }
    public long RequestedVersion { get; private set; }
    public long BookingVersionAfter { get; private set; }
    public string ResponseKind { get; private set; } = "";
    public string Decision { get; private set; } = "";
    public string StatusAfter { get; private set; } = "";
    public DateTime DeveloperRespondedAtUtc { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public Guid RecorderAccountId { get; private set; }
    public Guid? RecorderEmployeeId { get; private set; }
    public string Source { get; private set; } = "";
    public string Reference { get; private set; } = "";
    public decimal? OfferedPrice { get; private set; }
    public string? CurrencyCode { get; private set; }
    public DateTime? OfferedDeadlineUtc { get; private set; }
    public string? Reason { get; private set; }
    public long Version { get; private set; } = 1;
    public static bool ValidMoney(decimal value, string currency) => value is > 0 and <= MaxPrice &&
        decimal.Truncate(value*100) == value*100 && currency.Length == 3 && currency.All(c => c is >= 'A' and <= 'Z');
    public static BookingDeveloperResponse Record(Guid id, Booking booking, long requestedVersion, string kind, string decision,
        DateTime occurred, DateTime recorded, Guid actor, Guid? employee, string source, string reference,
        decimal? price, string? currency, DateTime? deadline, string? reason)
    {
        if (id == Guid.Empty || actor == Guid.Empty || employee == Guid.Empty || requestedVersion <= 0 ||
            booking.Version < requestedVersion || booking.Version-requestedVersion > 1 ||
            occurred == default || occurred.Kind != DateTimeKind.Utc || recorded.Kind != DateTimeKind.Utc ||
            occurred < booking.RequestedAtUtc || occurred > recorded ||
            string.IsNullOrWhiteSpace(source) || source.Trim().Length > SourceMaxLength ||
            string.IsNullOrWhiteSpace(reference) || reference.Trim().Length > ReferenceMaxLength)
            throw new ArgumentException("Actual bounded developer facts and recorder required.");
        if (kind == Booking.Confirmed)
        {
            if (price is not { } amount || currency is null || !ValidMoney(amount,currency) || deadline is not { } until ||
                until == default || until.Kind != DateTimeKind.Utc || reason is not null) throw new ArgumentException("Complete external offer required.");
        }
        else if (kind != Booking.Rejected || price is not null || currency is not null || deadline is not null ||
            string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > ReasonMaxLength) throw new ArgumentException("Developer rejection reason without offer fields required.");
        var changed = booking.Version > requestedVersion;
        var valid = decision switch {
            ConfirmedDecision => kind == Booking.Confirmed && booking.Status == Booking.Confirmed && changed && deadline > recorded,
            DeveloperRejected => kind == Booking.Rejected && booking.Status == Booking.Rejected && changed,
            InvalidDeadline => kind == Booking.Confirmed && booking.Status == Booking.Rejected && changed && deadline <= recorded,
            LocalConflict => kind == Booking.Confirmed && booking.Status == Booking.Rejected && changed && deadline > recorded,
            ResponseTimeoutDecision => booking.Status == Booking.Rejected && booking.RejectionReason == Booking.ResponseTimeout && changed,
            ReservationExpiredDecision => booking.Status == Booking.Expired && changed,
            StateUnchanged => booking.Status is Booking.Confirmed or Booking.Rejected or Booking.Expired or Booking.Cancelled or Booking.Completed && !changed &&
                (booking.Status!=Booking.Completed||booking.CompletedAtUtc is { } completed&&recorded>=completed),
            _ => false };
        if (!valid) throw new ArgumentException("Recorded decision must match committed booking effect.");
        return new() { Id = id, BookingId = booking.Id, CustomerId = booking.CustomerId, UnitId = booking.UnitId,
            RequestedVersion = requestedVersion, BookingVersionAfter = booking.Version, ResponseKind = kind, Decision = decision,
            StatusAfter = booking.Status, DeveloperRespondedAtUtc = occurred, RecordedAtUtc = recorded, RecorderAccountId = actor,
            RecorderEmployeeId = employee, Source = source.Trim(), Reference = reference.Trim(), OfferedPrice = price,
            CurrencyCode = currency, OfferedDeadlineUtc = deadline, Reason = reason?.Trim() };
    }
}
