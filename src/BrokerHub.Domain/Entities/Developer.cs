namespace BrokerHub.Domain.Entities;

public sealed class Developer
{
    public const int CodeMaxLength = 64;
    public const int NameMaxLength = 200;
    public const int LegalNameMaxLength = 300;
    public const int ContactNameMaxLength = 200;
    public const int ContactEmailMaxLength = 320;
    public const int ContactPhoneMaxLength = 32;

    private Developer()
    {
    }

    private Developer(
        Guid id,
        string code,
        string name,
        string? legalName,
        string? externalContactName,
        string? externalContactEmail,
        string? externalContactPhone,
        DateTime createdAtUtc,
        Guid createdByAccountId)
    {
        Id = id;
        Code = code;
        Name = name;
        LegalName = legalName;
        ExternalContactName = externalContactName;
        ExternalContactEmail = externalContactEmail;
        ExternalContactPhone = externalContactPhone;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        CreatedByAccountId = createdByAccountId;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? LegalName { get; private set; }

    public string? ExternalContactName { get; private set; }

    public string? ExternalContactEmail { get; private set; }

    public string? ExternalContactPhone { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid CreatedByAccountId { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByAccountId { get; private set; }

    public long Version { get; private set; }

    public static Developer Create(
        Guid id,
        string code,
        string name,
        string? legalName,
        string? externalContactName,
        string? externalContactEmail,
        string? externalContactPhone,
        DateTime createdAtUtc,
        Guid createdByAccountId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Developer ID is required.", nameof(id));
        }

        if (createdByAccountId == Guid.Empty)
        {
            throw new ArgumentException("Creating account ID is required.", nameof(createdByAccountId));
        }

        return new Developer(
            id,
            NormalizeRequired(code, CodeMaxLength, nameof(code)),
            NormalizeRequired(name, NameMaxLength, nameof(name)),
            NormalizeOptional(legalName, LegalNameMaxLength, nameof(legalName)),
            NormalizeOptional(externalContactName, ContactNameMaxLength, nameof(externalContactName)),
            NormalizeOptional(externalContactEmail, ContactEmailMaxLength, nameof(externalContactEmail)),
            NormalizeOptional(externalContactPhone, ContactPhoneMaxLength, nameof(externalContactPhone)),
            EnsureUtc(createdAtUtc),
            createdByAccountId);
    }

    public void UpdateDetails(
        string name,
        string? legalName,
        string? externalContactName,
        string? externalContactEmail,
        string? externalContactPhone,
        long expectedVersion,
        DateTime updatedAtUtc,
        Guid updatedByAccountId)
    {
        EnsureMutation(expectedVersion, updatedByAccountId);

        Name = NormalizeRequired(name, NameMaxLength, nameof(name));
        LegalName = NormalizeOptional(legalName, LegalNameMaxLength, nameof(legalName));
        ExternalContactName = NormalizeOptional(
            externalContactName,
            ContactNameMaxLength,
            nameof(externalContactName));
        ExternalContactEmail = NormalizeOptional(
            externalContactEmail,
            ContactEmailMaxLength,
            nameof(externalContactEmail));
        ExternalContactPhone = NormalizeOptional(
            externalContactPhone,
            ContactPhoneMaxLength,
            nameof(externalContactPhone));

        RecordMutation(updatedAtUtc, updatedByAccountId);
    }

    public bool Deactivate(long expectedVersion, DateTime updatedAtUtc, Guid updatedByAccountId)
    {
        EnsureMutation(expectedVersion, updatedByAccountId);
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        RecordMutation(updatedAtUtc, updatedByAccountId);
        return true;
    }

    public bool Reactivate(long expectedVersion, DateTime updatedAtUtc, Guid updatedByAccountId)
    {
        EnsureMutation(expectedVersion, updatedByAccountId);
        if (IsActive)
        {
            return false;
        }

        IsActive = true;
        RecordMutation(updatedAtUtc, updatedByAccountId);
        return true;
    }

    private void EnsureMutation(long expectedVersion, Guid updatedByAccountId)
    {
        if (expectedVersion <= 0 || expectedVersion != Version)
        {
            throw new InvalidOperationException("Developer version is stale.");
        }

        if (updatedByAccountId == Guid.Empty)
        {
            throw new ArgumentException("Updating account ID is required.", nameof(updatedByAccountId));
        }
    }

    private void RecordMutation(DateTime updatedAtUtc, Guid updatedByAccountId)
    {
        UpdatedAtUtc = EnsureUtc(updatedAtUtc);
        UpdatedByAccountId = updatedByAccountId;
        Version++;
    }

    private static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
