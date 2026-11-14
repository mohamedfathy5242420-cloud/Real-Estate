using System.Text.Json;
using BrokerHub.Application.Serialization;

namespace BrokerHub.Application.DTOs.Commands;

public sealed class CommandExecutionContext(Guid actorAccountId, Guid commandId, TimeProvider clock)
{
    private readonly List<OutboxEventIntent> _events = [];
    public Guid ActorAccountId { get; } = actorAccountId;
    public Guid CommandId { get; } = commandId;
    public IReadOnlyList<OutboxEventIntent> Events => _events.AsReadOnly();
    public void Emit(string name, int schemaVersion, Guid aggregateId, JsonElement explicitPayload)
    {
        if (!CanonicalCommandJson.IsIdentifier(name, 128, true) || schemaVersion <= 0 || aggregateId == Guid.Empty || _events.Count >= 100)
            throw new ArgumentException("Invalid bounded event identity/schema/count.");
        _events.Add(new(Guid.NewGuid(), name, schemaVersion, aggregateId, CanonicalCommandJson.Canonicalize(explicitPayload), clock.GetUtcNow().UtcDateTime));
    }
}
