using BrokerHub.Application.DTOs.Messaging;

namespace BrokerHub.Application.Interfaces.Messaging;

public interface IOutboxPublisher
{
    bool IsConfigured { get; }
    Task<OutboxPublishResult> PublishAsync(OutboxEnvelope envelope,CancellationToken ct);
}
