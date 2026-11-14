using BrokerHub.Application.DTOs.Notifications;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface INotificationCaptureStore
{
    Task<LocalNotificationCaptureResult> CaptureAsync(Guid recipientAccountId,Guid notificationId,CancellationToken ct);
}
