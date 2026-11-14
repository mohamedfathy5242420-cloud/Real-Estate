namespace BrokerHub.Domain.Entities;

public sealed class Employee
{
    public const int NumberMaxLength = 64;
    public const int NameMaxLength = 200;
    private readonly List<EmployeeRevision> _history = [];
    private Employee() { }
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string DisplayName { get; private set; } = "";
    public bool IsActive { get; private set; }
    public long Version { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid CreatedByAccountId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public IReadOnlyCollection<EmployeeRevision> History => _history.AsReadOnly();

    public static Employee Create(Guid id, Guid accountId, string number, string displayName, DateTime atUtc, Guid actor)
    {
        if (id == Guid.Empty || accountId == Guid.Empty || actor == Guid.Empty) throw new ArgumentException("Identities are required.");
        var employee = new Employee { Id = id, AccountId = accountId,
            EmployeeNumber = Text(number, NumberMaxLength).ToUpperInvariant(), DisplayName = Text(displayName, NameMaxLength),
            IsActive = true, Version = 1, CreatedAtUtc = Utc(atUtc), CreatedByAccountId = actor };
        employee._history.Add(EmployeeRevision.Capture(employee, atUtc, actor));
        return employee;
    }
    public void Update(string displayName, bool isActive, long expectedVersion, DateTime atUtc, Guid actor)
    {
        if (Version != expectedVersion || Version == long.MaxValue) throw new InvalidOperationException("Employee version changed or is exhausted.");
        if (actor == Guid.Empty) throw new ArgumentException("Actor is required.");
        var name = Text(displayName, NameMaxLength); atUtc = Utc(atUtc);
        if (atUtc < CreatedAtUtc || (UpdatedAtUtc.HasValue && atUtc < UpdatedAtUtc.Value)) throw new ArgumentException("Audit time precedes prior mutation.");
        DisplayName = name; IsActive = isActive; Version++; UpdatedAtUtc = atUtc; UpdatedByAccountId = actor;
        _history.Add(EmployeeRevision.Capture(this, atUtc, actor));
    }
    private static string Text(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max
        ? value.Trim() : throw new ArgumentException($"Nonblank text up to {max} characters required.");
    private static DateTime Utc(DateTime value) => value != default && value.Kind == DateTimeKind.Utc
        ? value : throw new ArgumentException("Explicit UTC time is required.");
}
