using BrokerHub.Application.DTOs.Messaging;
using BrokerHub.Application.Interfaces.Messaging;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Enums;

namespace BrokerHub.Application.Services;

public sealed class OutboxDispatcher(IOutboxDeliveryStore store,IOutboxPublisher publisher):IOutboxDispatcher
{
    public async Task<OutboxDispatchResult> DispatchBatchAsync(OutboxDispatchPolicy policy,CancellationToken ct)
    {
        if(!policy.IsValid)throw new ArgumentException("Invalid bounded outbox dispatch policy.",nameof(policy));
        ct.ThrowIfCancellationRequested();
        if(!publisher.IsConfigured)return new(false,0,0,0,0,0,0);
        var batch=await store.ClaimAsync(policy,ct);
        var published=0;var retry=0;var dead=batch.DeadLettered;var lost=0;
        foreach(var lease in batch.Leases)
        {
            ct.ThrowIfCancellationRequested();OutboxPublishResult result;
            if(!lease.Envelope.IsValid())result=new(OutboxPublishDisposition.PermanentFailure,OutboxFailureKind.InvalidEnvelope);
            else
            {
                try{result=await publisher.PublishAsync(lease.Envelope,ct);}
                catch(OperationCanceledException) when(ct.IsCancellationRequested){throw;}
                catch(Exception){result=new(OutboxPublishDisposition.RetryableFailure,OutboxFailureKind.TransportUnavailable);}
            }
            var completion=await store.CompleteAsync(lease.DeliveryId,lease.Token,result,policy,ct);
            switch(completion)
            {
                case OutboxCompletionStatus.Published:published++;break;
                case OutboxCompletionStatus.RetryScheduled:retry++;break;
                case OutboxCompletionStatus.DeadLettered:dead++;break;
                default:lost++;break;
            }
        }
        return new(true,batch.Inspected,batch.Leases.Count,published,retry,dead,lost);
    }
}
