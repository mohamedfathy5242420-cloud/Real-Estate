namespace BrokerHub.Domain.Entities;

public sealed class NotificationReadState
{
    private NotificationReadState() { }
    public Guid NotificationId { get; private set; }
    public DateTime NotificationCreatedAtUtc { get; private set; }
    public DateTime ReadAtUtc { get; private set; }
    public static NotificationReadState Record(Notification notification,DateTime atUtc)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if(notification.Id==Guid.Empty||atUtc.Kind!=DateTimeKind.Utc||atUtc<notification.CreatedAtUtc)
            throw new ArgumentException("Existing notification and consistent first-read UTC required.");
        return new(){NotificationId=notification.Id,NotificationCreatedAtUtc=DateTime.SpecifyKind(notification.CreatedAtUtc,DateTimeKind.Utc),ReadAtUtc=atUtc};
    }
}
