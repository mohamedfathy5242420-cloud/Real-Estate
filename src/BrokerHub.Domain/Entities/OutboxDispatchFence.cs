namespace BrokerHub.Domain.Entities;

// Technical lock row only; it never represents a business actor, receipt or event.
public sealed class OutboxDispatchFence
{
    private OutboxDispatchFence() { }
    public int Id { get; private set; }=1;
    public static OutboxDispatchFence Create()=>new();
}
