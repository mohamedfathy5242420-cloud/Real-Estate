namespace BrokerHub.Domain.Entities;

public sealed class InboxMessage
{
    public const string NotificationConsumer="BrokerHub.Notifications.v1";
    private InboxMessage() { }
    public Guid Id { get; private set; }
    public string ConsumerName { get; private set; }="";
    public Guid MessageId { get; private set; }
    public string BodyHash { get; private set; }="";
    public string SourceStore { get; private set; }="";
    public Guid SourceEventId { get; private set; }
    public string EventName { get; private set; }="";
    public int EventSchemaVersion { get; private set; }
    public int NotificationCount { get; private set; }
    public string RecipientDigest { get; private set; }="";
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime SourceRecordedAtUtc { get; private set; }
    public DateTime ProcessedAtUtc { get; private set; }

    public static InboxMessage Commit(Guid id,Guid messageId,string bodyHash,string sourceStore,
        Guid sourceEventId,string eventName,int schema,int count,string recipientDigest,
        DateTime occurredAtUtc,DateTime recordedAtUtc,DateTime processedAtUtc)
    {
        if(id==Guid.Empty||messageId==Guid.Empty||sourceEventId==Guid.Empty||
            sourceStore is not ("OutboxMessages" or "SystemOutboxMessages")||schema!=1||count<0||
            !Identifier(eventName,128)||!Hash(bodyHash)||!Hash(recipientDigest))
            throw new ArgumentException("Valid inbox source identities and bounded committed outcome required.");
        if(occurredAtUtc==default||occurredAtUtc.Kind!=DateTimeKind.Utc||recordedAtUtc.Kind!=DateTimeKind.Utc||
            processedAtUtc.Kind!=DateTimeKind.Utc||recordedAtUtc<occurredAtUtc||processedAtUtc<recordedAtUtc)
            throw new ArgumentException("Consistent inbox source and processing UTC required.");
        return new(){Id=id,ConsumerName=NotificationConsumer,MessageId=messageId,BodyHash=bodyHash,
            SourceStore=sourceStore,SourceEventId=sourceEventId,EventName=eventName,EventSchemaVersion=schema,
            NotificationCount=count,RecipientDigest=recipientDigest,OccurredAtUtc=occurredAtUtc,
            SourceRecordedAtUtc=recordedAtUtc,ProcessedAtUtc=processedAtUtc};
    }
    internal static bool Hash(string? value)=>value is {Length:64}&&value.All(c=>char.IsAsciiDigit(c)||c is >= 'A' and <= 'F');
    internal static bool Identifier(string? value,int max)=>!string.IsNullOrEmpty(value)&&value.Length<=max&&
        char.IsAsciiLetter(value[0])&&value.All(c=>char.IsAsciiLetterOrDigit(c)||c is '.' or '_' or ':' or '-');
}
