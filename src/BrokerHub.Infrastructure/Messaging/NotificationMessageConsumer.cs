using BrokerHub.Application.DTOs.Messaging;
using BrokerHub.Application.Interfaces.Messaging;
using BrokerHub.Application.Interfaces.Persistence;

namespace BrokerHub.Infrastructure.Messaging;

internal sealed class NotificationMessageConsumer(INotificationInboxStore store):INotificationMessageConsumer
{
    public Task<NotificationConsumeResult> ConsumeAsync(ReadOnlyMemory<byte> body,NotificationWireProperties properties,CancellationToken ct)=>
        NotificationEnvelopeReader.TryRead(body,properties,out var message)?store.ConsumeAsync(message,ct):
            Task.FromResult(new NotificationConsumeResult(NotificationConsumeStatus.Rejected));
}
