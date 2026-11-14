using BrokerHub.Application.DTOs.Messaging;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface INotificationInboxStore
{
    Task<NotificationConsumeResult> ConsumeAsync(ValidatedNotificationEnvelope message,CancellationToken ct);
}
