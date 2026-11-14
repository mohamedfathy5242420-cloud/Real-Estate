namespace BrokerHub.Domain.Entities;

public sealed class OutboxMessage
{
    private OutboxMessage() { }
    public Guid Id { get; private set; }
    public Guid CommandReceiptId { get; private set; }
    public Guid ActorAccountId { get; private set; }
    public string Name { get; private set; } = "";
    public int SchemaVersion { get; private set; }
    public Guid AggregateId { get; private set; }
    public string PayloadJson { get; private set; } = "";
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public static OutboxMessage Record(Guid id, Guid receiptId, Guid actor, string name, int schemaVersion,
        Guid aggregateId, string payload, DateTime occurredAtUtc, DateTime recordedAtUtc)
    {
        if (id == Guid.Empty || receiptId == Guid.Empty || actor == Guid.Empty || aggregateId == Guid.Empty || schemaVersion <= 0)
            throw new ArgumentException("Event identities/schema version required.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 128 || string.IsNullOrWhiteSpace(payload) || payload.Length > 16384)
            throw new ArgumentException("Invalid bounded event content.");
        if (occurredAtUtc == default || occurredAtUtc.Kind != DateTimeKind.Utc || recordedAtUtc.Kind != DateTimeKind.Utc || recordedAtUtc < occurredAtUtc)
            throw new ArgumentException("Explicit consistent UTC event times required.");
        return new() { Id = id, CommandReceiptId = receiptId, ActorAccountId = actor, Name = name, SchemaVersion = schemaVersion,
            AggregateId = aggregateId, PayloadJson = payload, OccurredAtUtc = occurredAtUtc, RecordedAtUtc = recordedAtUtc };
    }
}
