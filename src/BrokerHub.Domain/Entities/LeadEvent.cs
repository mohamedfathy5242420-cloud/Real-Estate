namespace BrokerHub.Domain.Entities;

public sealed class LeadEvent
{
    private LeadEvent() { }
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public Guid CustomerId { get; private set; }
    public long LeadVersion { get; private set; }
    public string EventType { get; private set; } = "";
    public Guid ActorAccountId { get; private set; }
    public DateTime AtUtc { get; private set; }
    public string? Reason { get; private set; }
    public static LeadEvent Record(Guid id, Lead lead, Guid actor, DateTime atUtc)
    {
        if (id == Guid.Empty || actor == Guid.Empty || atUtc == default || atUtc.Kind != DateTimeKind.Utc || atUtc < lead.OpenedAtUtc)
            throw new ArgumentException("Lead event audit required.");
        return new() { Id = id, LeadId = lead.Id, CustomerId = lead.CustomerId, LeadVersion = lead.Version,
            EventType = lead.Status == Lead.Open ? "Opened" : "Closed", ActorAccountId = actor, AtUtc = atUtc, Reason = lead.CloseReason };
    }
}
