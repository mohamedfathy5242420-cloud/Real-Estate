namespace BrokerHub.Domain.Entities;

public sealed class NotificationEmailCapture
{
    private NotificationEmailCapture() { }
    public Guid NotificationId { get; private set; }
    public DateTime NotificationCreatedAtUtc { get; private set; }
    public string ContentHash { get; private set; }="";
    public DateTime CapturedAtUtc { get; private set; }
    public static NotificationEmailCapture Record(Notification notification,string hash,DateTime atUtc)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if(notification.Id==Guid.Empty||!InboxMessage.Hash(hash)||atUtc.Kind!=DateTimeKind.Utc||atUtc<notification.CreatedAtUtc)
            throw new ArgumentException("Existing notification, exact artifact hash and consistent capture UTC required.");
        return new(){NotificationId=notification.Id,NotificationCreatedAtUtc=DateTime.SpecifyKind(notification.CreatedAtUtc,DateTimeKind.Utc),ContentHash=hash,CapturedAtUtc=atUtc};
    }
}
