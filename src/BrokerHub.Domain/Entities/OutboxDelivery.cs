using BrokerHub.Domain.Enums;

namespace BrokerHub.Domain.Entities;

public sealed class OutboxDelivery
{
    public const string Pending="Pending", Leased="Leased", Published="Published", DeadLetter="DeadLetter";
    private OutboxDelivery() { }
    public Guid Id { get; private set; }
    public Guid? HumanOutboxId { get; private set; }
    public Guid? SystemOutboxId { get; private set; }
    public DateTime EventRecordedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string State { get; private set; }=Pending;
    public int AttemptCount { get; private set; }
    public DateTime AvailableAfterUtc { get; private set; }
    public Guid? LeaseToken { get; private set; }
    public DateTime? LeaseExpiresAtUtc { get; private set; }
    public DateTime? LastAttemptAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public OutboxFailureKind LastFailureKind { get; private set; }
    public DateTime? LastFailureAtUtc { get; private set; }
    public long Version { get; private set; }=1;

    public static OutboxDelivery Capture(Guid id,Guid? humanId,Guid? systemId,DateTime eventRecorded,DateTime now)
    {
        if(id==Guid.Empty||humanId.HasValue==systemId.HasValue||humanId==Guid.Empty||systemId==Guid.Empty||
            eventRecorded==default||eventRecorded.Kind!=DateTimeKind.Utc||now.Kind!=DateTimeKind.Utc||now<eventRecorded)
            throw new ArgumentException("Exactly one original source and consistent UTC required.");
        return new(){Id=id,HumanOutboxId=humanId,SystemOutboxId=systemId,EventRecordedAtUtc=eventRecorded,
            CreatedAtUtc=now,AvailableAfterUtc=now};
    }
    public bool TryLease(Guid token,DateTime now,TimeSpan duration,int maxAttempts)
    {
        if(token==Guid.Empty||now.Kind!=DateTimeKind.Utc||now<CreatedAtUtc||now<LastAttemptAtUtc||duration<TimeSpan.FromSeconds(5)||
            duration>TimeSpan.FromHours(1)||maxAttempts is <1 or >20)throw new ArgumentException("Invalid dispatch lease policy or clock.");
        if(State is Published or DeadLetter||State==Pending&&AvailableAfterUtc>now||State==Leased&&LeaseExpiresAtUtc>now)return false;
        var expired=State==Leased;
        if(AttemptCount>=maxAttempts)
        {
            State=DeadLetter;Failure(expired?OutboxFailureKind.LeaseExpired:OutboxFailureKind.NotConfirmed,now);
            ClearLease();Version=checked(Version+1);return false;
        }
        if(expired)Failure(OutboxFailureKind.LeaseExpired,now);
        State=Leased;LeaseToken=token;LeaseExpiresAtUtc=now.Add(duration);LastAttemptAtUtc=now;
        AttemptCount=checked(AttemptCount+1);Version=checked(Version+1);return true;
    }
    public bool TryComplete(Guid token,DateTime now,bool confirmed,bool permanent,OutboxFailureKind failure,TimeSpan delay,int maxAttempts)
    {
        if(now.Kind!=DateTimeKind.Utc||delay<TimeSpan.Zero||delay>TimeSpan.FromHours(1)||maxAttempts is <1 or >20||!Enum.IsDefined(failure))
            throw new ArgumentException("Invalid dispatch completion policy.");
        if(State!=Leased||token==Guid.Empty||LeaseToken!=token||now<LastAttemptAtUtc||now>=LeaseExpiresAtUtc)return false;
        if(confirmed){State=Published;PublishedAtUtc=now;}
        else
        {
            Failure(failure==OutboxFailureKind.None?OutboxFailureKind.NotConfirmed:failure,now);
            State=permanent||AttemptCount>=maxAttempts?DeadLetter:Pending;
            AvailableAfterUtc=now.Add(delay);
        }
        ClearLease();Version=checked(Version+1);return true;
    }
    private void ClearLease(){LeaseToken=null;LeaseExpiresAtUtc=null;}
    private void Failure(OutboxFailureKind kind,DateTime now){LastFailureKind=kind;LastFailureAtUtc=now;}
}
