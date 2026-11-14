namespace BrokerHub.Domain.Entities;

public sealed class CustomerLinkRequestEvent
{
    private CustomerLinkRequestEvent() { }
    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public int Sequence { get; private set; }
    public string Kind { get; private set; } = "";
    public Guid ActorAccountId { get; private set; }
    public DateTime AtUtc { get; private set; }
    public string? Reason { get; private set; }
    public Guid? CustomerId { get; private set; }
    internal static CustomerLinkRequestEvent Create(Guid requestId, int sequence, string kind, Guid actor,
        DateTime atUtc, string? reason, Guid? customerId) => new() { Id = Guid.NewGuid(), RequestId = requestId,
        Sequence = sequence, Kind = kind, ActorAccountId = actor, AtUtc = atUtc, Reason = reason, CustomerId = customerId };
}
