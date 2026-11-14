namespace BrokerHub.Domain.Entities;

public sealed class Project
{
    public const int CodeMaxLength = 64;
    public const int NameMaxLength = 200;
    public const int AreaMaxLength = 200;
    public const int AddressMaxLength = 500;
    public const int TimeZoneIdMaxLength = 100;

    private Project()
    {
    }

    private Project(
        Guid id,
        Guid developerId,
        string code,
        string name,
        string area,
        string? address,
        string timeZoneId,
        DateTime createdAtUtc,
        Guid createdByAccountId)
    {
        Id = id;
        DeveloperId = developerId;
        Code = code;
        Name = name;
        Area = area;
        Address = address;
        TimeZoneId = timeZoneId;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        CreatedByAccountId = createdByAccountId;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid DeveloperId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Area { get; private set; } = string.Empty;

    public string? Address { get; private set; }

    public string TimeZoneId { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid CreatedByAccountId { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByAccountId { get; private set; }

    public long Version { get; private set; }

    public static Project Create(
        Guid id,
        Guid developerId,
        string code,
        string name,
        string area,
        string? address,
        string timeZoneId,
        DateTime createdAtUtc,
        Guid createdByAccountId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Project ID is required.", nameof(id));
        }

        if (developerId == Guid.Empty)
        {
            throw new ArgumentException("Developer ID is required.", nameof(developerId));
        }

        if (createdByAccountId == Guid.Empty)
        {
            throw new ArgumentException("Creating account ID is required.", nameof(createdByAccountId));
        }

        return new Project(
            id,
            developerId,
            NormalizeRequired(code, CodeMaxLength, nameof(code)),
            NormalizeRequired(name, NameMaxLength, nameof(name)),
            NormalizeRequired(area, AreaMaxLength, nameof(area)),
            NormalizeOptional(address, AddressMaxLength, nameof(address)),
            NormalizeRequired(timeZoneId, TimeZoneIdMaxLength, nameof(timeZoneId)),
            EnsureUtc(createdAtUtc),
            createdByAccountId);
    }

    public void UpdateDetails(
        string name,
        string area,
        string? address,
        string timeZoneId,
        long expectedVersion,
        DateTime updatedAtUtc,
        Guid updatedByAccountId)
    {
        EnsureMutation(expectedVersion, updatedByAccountId);

        Name = NormalizeRequired(name, NameMaxLength, nameof(name));
        Area = NormalizeRequired(area, AreaMaxLength, nameof(area));
        Address = NormalizeOptional(address, AddressMaxLength, nameof(address));
        TimeZoneId = NormalizeRequired(timeZoneId, TimeZoneIdMaxLength, nameof(timeZoneId));

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
            throw new InvalidOperationException("Project version is stale.");
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
