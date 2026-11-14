namespace BrokerHub.Domain.Entities;

public sealed class UnitMarketConfirmation
{
    public const int SourceMaxLength = 200;
    public const int ReferenceMaxLength = 500;
    public const int NotesMaxLength = 4000;
    public const string Available = "Available";
    public const string Unavailable = "Unavailable";
    private UnitMarketConfirmation() { }
    public Guid Id { get; private set; }
    public Guid UnitId { get; private set; }
    public DateTime ConfirmedAtUtc { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public Guid RecordedByAccountId { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string? SourceReference { get; private set; }
    public string? Notes { get; private set; }
    public decimal? ConfirmedPrice { get; private set; }
    public string? Currency { get; private set; }
    public string? AvailabilityStatus { get; private set; }

    public static UnitMarketConfirmation Create(Guid id, Guid unitId, DateTime confirmedAtUtc,
        DateTime recordedAtUtc, Guid actor, string source, string? reference, string? notes,
        decimal? price, string? currency, string? availability)
    {
        if (id == Guid.Empty || unitId == Guid.Empty || actor == Guid.Empty)
            throw new ArgumentException("Confirmation, Unit and recorder identifiers are required.");
        if (confirmedAtUtc.Kind != DateTimeKind.Utc || recordedAtUtc.Kind != DateTimeKind.Utc ||
            confirmedAtUtc == default || confirmedAtUtc > recordedAtUtc)
            throw new ArgumentException("External confirmation must be a nondefault UTC fact no later than recording time.", nameof(confirmedAtUtc));
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        if (source.Trim().Length > SourceMaxLength) throw new ArgumentException("Source exceeds storage limit.", nameof(source));
        var normalizedCurrency = Optional(currency, 3, nameof(currency))?.ToUpperInvariant();
        if (price is null && normalizedCurrency is not null) throw new ArgumentException("Currency requires a confirmed price.", nameof(currency));
        if (price is not null && (price < 0 || price > Unit.DecimalMaximum || decimal.Round(price.Value, 2) != price))
            throw new ArgumentException("Confirmed price must fit nonnegative decimal(18,2).", nameof(price));
        if (price is not null && (normalizedCurrency is null || normalizedCurrency.Length != 3 ||
            normalizedCurrency.Any(c => c is < 'A' or > 'Z')))
            throw new ArgumentException("Confirmed money requires a three-letter alphabetic currency.", nameof(currency));
        if (availability is not null && availability is not (Available or Unavailable))
            throw new ArgumentException("Only Available/Unavailable external observations are supported.", nameof(availability));
        if (price is null && availability is null) throw new ArgumentException("At least price or availability is required.");
        return new UnitMarketConfirmation
        {
            Id = id, UnitId = unitId, ConfirmedAtUtc = confirmedAtUtc, RecordedAtUtc = recordedAtUtc,
            RecordedByAccountId = actor, Source = source.Trim(),
            SourceReference = Optional(reference, ReferenceMaxLength, nameof(reference)),
            Notes = Optional(notes, NotesMaxLength, nameof(notes)), ConfirmedPrice = price,
            Currency = normalizedCurrency, AvailabilityStatus = availability
        };
    }
    private static string? Optional(string? value, int maximum, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Trim().Length > maximum) throw new ArgumentException("Value exceeds storage limit.", field);
        return value.Trim();
    }
}
