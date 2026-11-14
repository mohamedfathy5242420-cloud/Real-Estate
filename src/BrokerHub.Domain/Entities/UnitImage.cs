namespace BrokerHub.Domain.Entities;

public sealed class UnitImage
{
    public const int AltTextMaxLength = 500;
    public const int StorageKeyMaxLength = 64;
    private UnitImage() { }
    public Guid Id { get; private set; }
    public Guid UnitId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid CreatedByAccountId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }

    public static UnitImage Create(Guid id, Guid unitId, string storageKey, string? altText,
        int sortOrder, DateTime utc, Guid actor)
    {
        if (id == Guid.Empty || unitId == Guid.Empty || actor == Guid.Empty)
            throw new ArgumentException("Image, Unit and actor IDs are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        if (storageKey.Length > StorageKeyMaxLength) throw new ArgumentException("Storage key exceeds storage limit.");
        ValidateMetadata(altText, sortOrder);
        return new UnitImage { Id = id, UnitId = unitId, StorageKey = storageKey,
            AltText = Normalize(altText), SortOrder = sortOrder, IsPrimary = false,
            CreatedAtUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc), CreatedByAccountId = actor };
    }

    public void UpdateMetadata(string? altText, int sortOrder, DateTime utc, Guid actor)
    {
        ValidateMetadata(altText, sortOrder);
        if (actor == Guid.Empty) throw new ArgumentException("Actor ID is required.", nameof(actor));
        AltText = Normalize(altText); SortOrder = sortOrder;
        UpdatedAtUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc); UpdatedByAccountId = actor;
    }

    public static void ValidateMetadata(string? altText, int sortOrder)
    {
        if (!string.IsNullOrWhiteSpace(altText) && altText.Trim().Length > AltTextMaxLength)
            throw new ArgumentException("Alt text exceeds 500 characters.", nameof(altText));
        if (sortOrder < 0) throw new ArgumentException("Sort order cannot be negative.", nameof(sortOrder));
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
