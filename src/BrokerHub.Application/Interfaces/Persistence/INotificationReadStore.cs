using BrokerHub.Application.DTOs.Notifications;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface INotificationReadStore
{
    Task<NotificationResult<NotificationReadReply>> MarkReadAsync(Guid actor,Guid notificationId,CancellationToken ct);
}
