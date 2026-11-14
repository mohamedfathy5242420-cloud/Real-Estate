using BrokerHub.Application.DTOs.Messaging;
using BrokerHub.Application.Interfaces.Messaging;

namespace BrokerHub.Infrastructure.Messaging;

internal sealed class UnconfiguredOutboxPublisher:IOutboxPublisher
{
    public bool IsConfigured=>false;
    public Task<OutboxPublishResult> PublishAsync(OutboxEnvelope envelope,CancellationToken ct)=>
        throw new InvalidOperationException("No outbox transport is configured.");
}
