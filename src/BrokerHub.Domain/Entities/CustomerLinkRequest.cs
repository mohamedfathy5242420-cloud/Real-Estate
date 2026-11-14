using BrokerHub.Domain.Enums;

namespace BrokerHub.Domain.Entities;

public sealed class CustomerLinkRequest
{
    public const int TextMaxLength = 2000;
    private readonly List<CustomerLinkRequestEvent> _history = [];
    private CustomerLinkRequest() { }
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public string Claim { get; private set; } = "";
    public CustomerLinkRequestStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DecidedAtUtc { get; private set; }
    public Guid? DecidedByAccountId { get; private set; }
    public string? DecisionReason { get; private set; }
    public Guid? CustomerId { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<CustomerLinkRequestEvent> History => _history.AsReadOnly();

    public static CustomerLinkRequest Submit(Guid id, Guid accountId, string claim, DateTime atUtc)
    {
        if (id == Guid.Empty || accountId == Guid.Empty) throw new ArgumentException("Identities are required.");
        var request = new CustomerLinkRequest { Id = id, AccountId = accountId, Claim = Text(claim),
            CreatedAtUtc = Utc(atUtc), Status = CustomerLinkRequestStatus.Pending, Version = 1 };
        request._history.Add(CustomerLinkRequestEvent.Create(id, 1, "Submitted", accountId, atUtc, null, null));
        return request;
    }

    public void Decide(bool approve, Guid? customerId, long expectedVersion, string reason, DateTime atUtc, Guid actor)
    {
        if (Status != CustomerLinkRequestStatus.Pending || expectedVersion != Version)
            throw new InvalidOperationException("Request is terminal or its version changed.");
        if (actor == Guid.Empty || (approve ? !customerId.HasValue || customerId == Guid.Empty : customerId.HasValue))
            throw new ArgumentException("Decision identities are invalid.");
        atUtc = Utc(atUtc);
        if (atUtc < CreatedAtUtc) throw new ArgumentException("Decision precedes submission.");
        reason = Text(reason);
        Status = approve ? CustomerLinkRequestStatus.Approved : CustomerLinkRequestStatus.Rejected;
        CustomerId = customerId; DecisionReason = reason; DecidedAtUtc = atUtc; DecidedByAccountId = actor; Version++;
        _history.Add(CustomerLinkRequestEvent.Create(Id, 2, Status.ToString(), actor, atUtc, reason, customerId));
    }
    private static string Text(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= TextMaxLength
        ? value.Trim() : throw new ArgumentException("A nonblank text of at most 2000 characters is required.", nameof(value));
    private static DateTime Utc(DateTime value) => value != default && value.Kind == DateTimeKind.Utc
        ? value : throw new ArgumentException("Explicit UTC time is required.", nameof(value));
}
