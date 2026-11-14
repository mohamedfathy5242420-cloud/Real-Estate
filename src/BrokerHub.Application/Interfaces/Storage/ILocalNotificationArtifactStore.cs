using BrokerHub.Application.DTOs.Notifications;

namespace BrokerHub.Application.Interfaces.Storage;

public interface ILocalNotificationArtifactStore
{
    Task EnsureAsync(LocalNotificationArtifact artifact,CancellationToken ct);
    Task<bool> VerifyAsync(LocalNotificationArtifact artifact,CancellationToken ct);
}
