namespace BrokerHub.Domain.Entities;

public sealed class NotificationProcessingAudit
{
    private NotificationProcessingAudit() { }
    public Guid InboxMessageId { get; private set; }
    public Guid MessageId { get; private set; }
    public string ConsumerName { get; private set; }="";
    public string SourceStore { get; private set; }="";
    public Guid SourceEventId { get; private set; }
    public Guid? SourceActorAccountId { get; private set; }
    public string? SystemSource { get; private set; }
    public string EventName { get; private set; }="";
    public int EventSchemaVersion { get; private set; }
    public string BodyHash { get; private set; }="";
    public int NotificationCount { get; private set; }
    public string RecipientDigest { get; private set; }="";
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime ProcessedAtUtc { get; private set; }

    public static NotificationProcessingAudit Record(InboxMessage inbox,Guid? originalActor,string? systemSource)
    {
        ArgumentNullException.ThrowIfNull(inbox);
        if(inbox.SourceStore=="OutboxMessages"?
            originalActor is null||originalActor==Guid.Empty||systemSource is not null:
            originalActor is not null||!InboxMessage.Identifier(systemSource,64))
            throw new ArgumentException("Truthful original human or actorless System provenance required.");
        return new(){InboxMessageId=inbox.Id,MessageId=inbox.MessageId,ConsumerName=inbox.ConsumerName,
            SourceStore=inbox.SourceStore,SourceEventId=inbox.SourceEventId,SourceActorAccountId=originalActor,
            SystemSource=systemSource,EventName=inbox.EventName,EventSchemaVersion=inbox.EventSchemaVersion,
            BodyHash=inbox.BodyHash,NotificationCount=inbox.NotificationCount,RecipientDigest=inbox.RecipientDigest,
            OccurredAtUtc=inbox.OccurredAtUtc,ProcessedAtUtc=inbox.ProcessedAtUtc};
    }
}
