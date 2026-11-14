using BrokerHub.Application.DTOs.Messaging;

namespace BrokerHub.Application.Interfaces.Messaging;

public interface INotificationMessageConsumer
{
    Task<NotificationConsumeResult> ConsumeAsync(ReadOnlyMemory<byte> body,NotificationWireProperties properties,CancellationToken ct);
}
