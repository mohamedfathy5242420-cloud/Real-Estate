using BrokerHub.Application.DTOs.Commands;

namespace BrokerHub.Application.DTOs.Notifications;

public sealed record NotificationResourceDto(string Kind,Guid Id,Guid? CustomerId,Guid? ParentId,Guid? RelatedId);
public sealed record UserNotificationDto(Guid Id,string Kind,string Title,string Message,DateTimeOffset OccurredAtUtc,
    DateTimeOffset CreatedAtUtc,DateTimeOffset? ReadAtUtc,NotificationResourceDto Resource);
public sealed record UserNotificationPage(IReadOnlyList<UserNotificationDto> Items,int TotalCount,int Page,int PageSize);
public sealed record NotificationUnreadCountDto(int UnreadCount);
public sealed record NotificationReadReply(Guid NotificationId,DateTimeOffset ReadAtUtc);
public sealed record NotificationListFilter(int Page=1,int PageSize=20,string? ReadStatus=null)
{
    public bool IsValid=>Page is >=1 and <=1000000&&PageSize is >=1 and <=100&&ReadStatus is null or "read" or "unread";
}
public sealed record NotificationResult<T>(CommandStatus Status,T? Value=default);
public enum LocalNotificationCaptureStatus { Success, Disabled, Forbidden, NotFound, Conflict, Invalid }
public sealed record LocalNotificationCaptureResult(LocalNotificationCaptureStatus Status,Guid? NotificationId=null,
    DateTimeOffset? CapturedAtUtc=null,string? ContentHash=null);
public sealed record LocalNotificationArtifact(Guid NotificationId,byte[] Bytes,string ContentHash);
