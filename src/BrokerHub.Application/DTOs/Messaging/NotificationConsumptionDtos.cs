namespace BrokerHub.Application.DTOs.Messaging;

public sealed record NotificationWireProperties(string? MessageId,string? ContentType,string? Type,bool Persistent);
public sealed record ValidatedNotificationEnvelope(OutboxEnvelope Envelope,string BodyHash);
public enum NotificationConsumeStatus { Applied, Duplicate, Rejected }
public sealed record NotificationConsumeResult(NotificationConsumeStatus Status,int NotificationCount=0);
