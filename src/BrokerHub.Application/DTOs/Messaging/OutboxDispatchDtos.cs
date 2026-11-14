using System.Text.Json;
using BrokerHub.Domain.Enums;

namespace BrokerHub.Application.DTOs.Messaging;

public sealed record OutboxDispatchPolicy(int BatchSize=20,int LeaseSeconds=60,int MaxAttempts=5,int RetryBaseSeconds=5,int RetryMaxSeconds=300)
{
    public bool IsValid=>BatchSize is >=1 and <=100&&LeaseSeconds is >=5 and <=3600&&MaxAttempts is >=1 and <=20&&
        RetryBaseSeconds is >=1 and <=3600&&RetryMaxSeconds>=RetryBaseSeconds&&RetryMaxSeconds<=3600;
    public TimeSpan RetryDelay(int attempt)=>TimeSpan.FromSeconds(Math.Min(RetryMaxSeconds,RetryBaseSeconds*Math.Pow(2,Math.Clamp(attempt-1,0,19))));
}
public sealed record OutboxEnvelope(int EnvelopeVersion,Guid MessageId,string SourceStore,Guid SourceEventId,string Name,
    int EventSchemaVersion,Guid AggregateId,Guid? ActorAccountId,string? SystemSource,Guid? ResourceId,long? ResourceVersion,
    DateTime OccurredAtUtc,DateTime RecordedAtUtc,string PayloadJson)
{
    private static bool Identifier(string? value,int max)=>!string.IsNullOrEmpty(value)&&value.Length<=max&&char.IsAsciiLetter(value[0])&&
        value.All(c=>char.IsAsciiLetterOrDigit(c)||c is '.' or '_' or ':' or '-');
    public bool IsValid()
    {
        if(EnvelopeVersion!=1||MessageId==Guid.Empty||SourceEventId==Guid.Empty||AggregateId==Guid.Empty||EventSchemaVersion<=0||
            !Identifier(Name,128)||OccurredAtUtc==default||OccurredAtUtc.Kind!=DateTimeKind.Utc||RecordedAtUtc.Kind!=DateTimeKind.Utc||
            RecordedAtUtc<OccurredAtUtc||string.IsNullOrWhiteSpace(PayloadJson)||PayloadJson.Length>16384)return false;
        if(SourceStore=="OutboxMessages")
        {if(ActorAccountId is null||ActorAccountId==Guid.Empty||SystemSource is not null||ResourceId is not null||ResourceVersion is not null)return false;}
        else if(SourceStore=="SystemOutboxMessages")
        {if(ActorAccountId is not null||!Identifier(SystemSource,64)||ResourceId is null||ResourceId==Guid.Empty||ResourceVersion is null or <=0)return false;}
        else return false;
        try{using var json=JsonDocument.Parse(PayloadJson);return json.RootElement.ValueKind==JsonValueKind.Object;}
        catch(JsonException){return false;}
    }
}
public enum OutboxPublishDisposition { Confirmed, RetryableFailure, Uncertain, PermanentFailure }
public sealed record OutboxPublishResult(OutboxPublishDisposition Disposition,OutboxFailureKind Failure=OutboxFailureKind.None);
public sealed record OutboxLease(Guid DeliveryId,Guid Token,int Attempt,OutboxEnvelope Envelope);
public sealed record OutboxClaimBatch(int Inspected,int DeadLettered,IReadOnlyList<OutboxLease> Leases);
public enum OutboxCompletionStatus { LeaseLost, Published, RetryScheduled, DeadLettered }
public sealed record OutboxDispatchResult(bool PublisherConfigured,int Inspected,int Claimed,int Published,int RetryScheduled,int DeadLettered,int LeaseLost);
