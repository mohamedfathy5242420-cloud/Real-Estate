using BrokerHub.Application.DTOs.Notifications;

namespace BrokerHub.Application.Interfaces.Services;

public interface IUserNotificationService
{
    Task<bool> CanAccessAsync(CancellationToken ct);
    Task<NotificationResult<UserNotificationPage>> ListAsync(NotificationListFilter filter,CancellationToken ct);
    Task<NotificationResult<UserNotificationDto>> GetAsync(Guid id,CancellationToken ct);
    Task<NotificationResult<NotificationUnreadCountDto>> UnreadCountAsync(CancellationToken ct);
    Task<NotificationResult<NotificationReadReply>> MarkReadAsync(Guid id,CancellationToken ct);
}
