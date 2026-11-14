namespace BrokerHub.Domain.Entities;

public sealed class Unit
{
    public const int CodeMaxLength = 64;
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 4000;
    public const int UnitTypeMaxLength = 100;
    public const decimal DecimalMaximum = 9999999999999999.99m;
    // Publication is separate from external availability; S08 adds only explicit publication transitions.
    public const string Unpublished = "Unpublished";
    public const string Published = "Published";
    public const string Unconfirmed = "Unconfirmed";

    private Unit() { }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string UnitType { get; private set; } = string.Empty;
    public decimal AreaSqm { get; private set; }
    public int? Bedrooms { get; private set; }
    public int? Bathrooms { get; private set; }
    public decimal ListPrice { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string PublicationStatus { get; private set; } = Unpublished;
    public string AvailabilityStatus { get; private set; } = Unconfirmed;
    public DateTime? LastPriceConfirmedAtUtc { get; private set; }
    public string? LastPriceSource { get; private set; }
    public DateTime? LastAvailabilityConfirmedAtUtc { get; private set; }
    public string? LastAvailabilitySource { get; private set; }
    public Guid? LatestPriceConfirmationId { get; private set; }
    public decimal? ConfirmedPrice { get; private set; }
    public string? ConfirmedCurrency { get; private set; }
    public Guid? LatestAvailabilityConfirmationId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid CreatedByAccountId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public long Version { get; private set; }

    public static Unit Create(Guid id, Guid projectId, string code, string title,
        string? description, string unitType, decimal areaSqm, int? bedrooms, int? bathrooms,
        decimal listPrice, string currency, DateTime createdAtUtc, Guid createdByAccountId)
    {
        if (id == Guid.Empty) throw new ArgumentException("Unit ID is required.", nameof(id));
        if (projectId == Guid.Empty) throw new ArgumentException("Project ID is required.", nameof(projectId));
        if (createdByAccountId == Guid.Empty) throw new ArgumentException("Actor ID is required.", nameof(createdByAccountId));
        ValidateDetails(title, description, unitType, areaSqm, bedrooms, bathrooms, listPrice, currency);
        var normalizedCode = NormalizeCode(code);
        var unit = new Unit
        {
            Id = id, ProjectId = projectId, Code = normalizedCode,
            CreatedAtUtc = EnsureUtc(createdAtUtc), CreatedByAccountId = createdByAccountId, Version = 1
        };
        unit.AssignDetails(title, description, unitType, areaSqm, bedrooms, bathrooms, listPrice, currency);
        return unit;
    }

    public void UpdateDetails(string title, string? description, string unitType, decimal areaSqm,
        int? bedrooms, int? bathrooms, decimal listPrice, string currency, long expectedVersion,
        DateTime updatedAtUtc, Guid updatedByAccountId)
    {
        if (expectedVersion <= 0 || expectedVersion != Version)
            throw new InvalidOperationException("Unit version is stale.");
        if (updatedByAccountId == Guid.Empty) throw new ArgumentException("Actor ID is required.", nameof(updatedByAccountId));
        ValidateDetails(title, description, unitType, areaSqm, bedrooms, bathrooms, listPrice, currency);
        if (Version == long.MaxValue) throw new InvalidOperationException("Unit version is exhausted.");
        AssignDetails(title, description, unitType, areaSqm, bedrooms, bathrooms, listPrice, currency);
        UpdatedAtUtc = EnsureUtc(updatedAtUtc);
        UpdatedByAccountId = updatedByAccountId;
        Version++;
    }

    // Image metadata is owned by the Unit aggregate; serialize collection and detail writes on its Version.
    public void RecordImageChange(long expectedVersion, DateTime updatedAtUtc, Guid updatedByAccountId)
    {
        if (expectedVersion <= 0 || expectedVersion != Version || Version == long.MaxValue)
            throw new InvalidOperationException("Unit collection version is stale or exhausted.");
        if (updatedByAccountId == Guid.Empty)
            throw new ArgumentException("Actor ID is required.", nameof(updatedByAccountId));
        UpdatedAtUtc = EnsureUtc(updatedAtUtc);
        UpdatedByAccountId = updatedByAccountId;
        Version++;
    }

    public static string NormalizeCode(string code) => Required(code, CodeMaxLength, nameof(code));
    public UnitPublicationTransition ChangePublication(bool publish, long expectedVersion, DateTime at,
        Guid actor, string? reason, Guid? agreementId = null, DateOnly? date = null, string? zone = null)
    {
        if (expectedVersion <= 0 || expectedVersion != Version || Version == long.MaxValue)
            throw new InvalidOperationException("Unit version is stale or exhausted.");
        if (PublicationStatus != (publish ? Unpublished : Published))
            throw new InvalidOperationException("Invalid publication transition.");
        var transition = UnitPublicationTransition.Create(this, publish, Version + 1, at, actor, reason, agreementId, date, zone);
        PublicationStatus = transition.ToStatus;
        UpdatedAtUtc = at; UpdatedByAccountId = actor; Version++;
        return transition;
    }
    public void RecordMarketConfirmation(UnitMarketConfirmation confirmation, long expectedVersion)
    {
        if (confirmation.UnitId != Id) throw new ArgumentException("Confirmation must belong to this Unit.");
        if (expectedVersion <= 0 || expectedVersion != Version || Version == long.MaxValue)
            throw new InvalidOperationException("Unit version is stale or exhausted.");
        // Strictly newer external time per component; equal/older observations remain immutable history.
        if (confirmation.ConfirmedPrice.HasValue &&
            (!LastPriceConfirmedAtUtc.HasValue || confirmation.ConfirmedAtUtc > LastPriceConfirmedAtUtc.Value))
        {
            LatestPriceConfirmationId = confirmation.Id;
            ConfirmedPrice = confirmation.ConfirmedPrice; ConfirmedCurrency = confirmation.Currency;
            LastPriceConfirmedAtUtc = confirmation.ConfirmedAtUtc; LastPriceSource = confirmation.Source;
        }
        if (confirmation.AvailabilityStatus is not null &&
            (!LastAvailabilityConfirmedAtUtc.HasValue || confirmation.ConfirmedAtUtc > LastAvailabilityConfirmedAtUtc.Value))
        {
            LatestAvailabilityConfirmationId = confirmation.Id;
            AvailabilityStatus = confirmation.AvailabilityStatus;
            LastAvailabilityConfirmedAtUtc = confirmation.ConfirmedAtUtc; LastAvailabilitySource = confirmation.Source;
        }
        UpdatedAtUtc = confirmation.RecordedAtUtc; UpdatedByAccountId = confirmation.RecordedByAccountId;
        Version++;
    }

    public static void ValidateDetails(string title, string? description, string unitType, decimal areaSqm,
        int? bedrooms, int? bathrooms, decimal listPrice, string currency)
    {
        Required(title, TitleMaxLength, nameof(title));
        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > DescriptionMaxLength)
            throw new ArgumentException("Description exceeds storage limit.", nameof(description));
        Required(unitType, UnitTypeMaxLength, nameof(unitType));
        ValidateDecimal(areaSqm, positive: true, nameof(areaSqm));
        ValidateDecimal(listPrice, positive: false, nameof(listPrice));
        if (bedrooms < 0) throw new ArgumentException("Bedrooms cannot be negative.", nameof(bedrooms));
        if (bathrooms < 0) throw new ArgumentException("Bathrooms cannot be negative.", nameof(bathrooms));
        var normalized = Required(currency, 3, nameof(currency)).ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must be a three-letter ISO alphabetic representation.", nameof(currency));
    }

    private void AssignDetails(string title, string? description, string unitType, decimal areaSqm,
        int? bedrooms, int? bathrooms, decimal listPrice, string currency)
    {
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UnitType = unitType.Trim();
        AreaSqm = areaSqm;
        Bedrooms = bedrooms;
        Bathrooms = bathrooms;
        ListPrice = listPrice;
        Currency = currency.Trim().ToUpperInvariant();
        // Advertisement edits do not assert external price, availability or publication facts.
    }

    private static void ValidateDecimal(decimal value, bool positive, string field)
    {
        if ((positive ? value <= 0 : value < 0) || value > DecimalMaximum || decimal.Round(value, 2) != value)
            throw new ArgumentException("Value is outside decimal(18,2) bounds or has more than two fractional digits.", field);
    }

    private static string Required(string value, int length, string field)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, field);
        var normalized = value.Trim();
        if (normalized.Length > length) throw new ArgumentException($"Value cannot exceed {length} characters.", field);
        return normalized;
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
