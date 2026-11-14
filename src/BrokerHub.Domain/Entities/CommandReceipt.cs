namespace BrokerHub.Domain.Entities;

public sealed class CommandReceipt
{
    private CommandReceipt() { }
    public Guid Id { get; private set; }
    public Guid ActorAccountId { get; private set; }
    public string Operation { get; private set; } = "";
    public string IdempotencyKey { get; private set; } = "";
    public string RequestHash { get; private set; } = "";
    public int FingerprintVersion { get; private set; }
    public string ResourceType { get; private set; } = "";
    public Guid ResourceId { get; private set; }
    public long ResourceVersion { get; private set; }
    public int EventCount { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }
    public static CommandReceipt Create(Guid id, Guid actor, string operation, string key, string hash,
        string resourceType, Guid resourceId, long resourceVersion, int eventCount, DateTime completedAtUtc)
    {
        if (id == Guid.Empty || actor == Guid.Empty || resourceId == Guid.Empty || resourceVersion <= 0 || eventCount is < 0 or > 100)
            throw new ArgumentException("Invalid command outcome identity/version/count.");
        if (completedAtUtc == default || completedAtUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Explicit UTC completion required.");
        if (string.IsNullOrWhiteSpace(hash) || hash.Length != 64 || hash.Any(c => !char.IsAsciiHexDigitUpper(c))) throw new ArgumentException("SHA256 uppercase hex required.");
        return new() { Id = id, ActorAccountId = actor, Operation = Text(operation, 64), IdempotencyKey = Text(key, 128),
            RequestHash = hash, FingerprintVersion = 1, ResourceType = Text(resourceType, 64), ResourceId = resourceId,
            ResourceVersion = resourceVersion, EventCount = eventCount, CompletedAtUtc = completedAtUtc };
    }
    private static string Text(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Length <= max
        ? value : throw new ArgumentException("Invalid bounded command text.");
}
