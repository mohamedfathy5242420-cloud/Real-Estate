using BrokerHub.Application.DTOs.Notifications;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IUserNotificationQueries
{
    Task<bool> CanAccessAsync(Guid actor,CancellationToken ct);
    Task<NotificationResult<UserNotificationPage>> ListAsync(Guid actor,NotificationListFilter filter,CancellationToken ct);
    Task<NotificationResult<UserNotificationDto>> GetAsync(Guid actor,Guid id,CancellationToken ct);
    Task<NotificationResult<NotificationUnreadCountDto>> UnreadCountAsync(Guid actor,CancellationToken ct);
}
