namespace BrokerHub.Domain.Entities;

public sealed class SystemOutboxMessage
{
    private SystemOutboxMessage() { }
    public Guid Id { get; private set; }
    public string Source { get; private set; } = "";
    public string Name { get; private set; } = "";
    public int SchemaVersion { get; private set; }
    public Guid AggregateId { get; private set; }
    public Guid ResourceId { get; private set; }
    public long ResourceVersion { get; private set; }
    public string PayloadJson { get; private set; } = "";
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public static SystemOutboxMessage Record(Guid id, string source, string name, int schemaVersion,
        Guid aggregateId, Guid resourceId, long version, string payload, DateTime occurredAt, DateTime recordedAt)
    {
        static bool Identifier(string s, int max) => !string.IsNullOrEmpty(s) && s.Length <= max && char.IsAsciiLetter(s[0]) && s.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or ':' or '-');
        if (id == Guid.Empty || aggregateId == Guid.Empty || resourceId == Guid.Empty || version <= 0 || schemaVersion <= 0 ||
            !Identifier(source,64) || !Identifier(name,128) || string.IsNullOrWhiteSpace(payload) || payload.Length > 16384)
            throw new ArgumentException("Bounded system event identities/content required.");
        if (occurredAt == default || occurredAt.Kind != DateTimeKind.Utc || recordedAt.Kind != DateTimeKind.Utc || recordedAt < occurredAt)
            throw new ArgumentException("Consistent system event UTC required.");
        return new() { Id = id, Source = source, Name = name, SchemaVersion = schemaVersion, AggregateId = aggregateId,
            ResourceId = resourceId, ResourceVersion = version, PayloadJson = payload, OccurredAtUtc = occurredAt, RecordedAtUtc = recordedAt };
    }
}
