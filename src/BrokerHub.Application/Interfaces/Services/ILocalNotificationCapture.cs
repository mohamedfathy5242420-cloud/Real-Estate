using BrokerHub.Application.DTOs.Notifications;

namespace BrokerHub.Application.Interfaces.Services;

public interface ILocalNotificationCapture
{
    Task<LocalNotificationCaptureResult> CaptureAsync(Guid recipientAccountId,Guid notificationId,CancellationToken ct);
}
