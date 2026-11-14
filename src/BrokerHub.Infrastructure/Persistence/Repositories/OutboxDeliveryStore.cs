using System.Data.SqlTypes;
using BrokerHub.Application.DTOs.Messaging;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;

internal sealed class OutboxDeliveryStore(ApplicationDbContext db,TimeProvider clock):IOutboxDeliveryStore
{
    private static DateTime Utc(DateTime at)=>DateTime.SpecifyKind(at,DateTimeKind.Utc);
    private void OwnScope()
    {
        if(db.Database.CurrentTransaction is not null||db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Outbox delivery requires its own clean transaction scope.");
    }
    private async Task Fence(CancellationToken ct)=>_ = await db.OutboxDispatchFences
        .FromSqlRaw("SELECT [Id] FROM [Reliability].[OutboxDispatchFences] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]=1")
        .AsNoTracking().SingleAsync(ct);
    public async Task<OutboxClaimBatch> ClaimAsync(OutboxDispatchPolicy policy,CancellationToken ct)
    {
        if(!policy.IsValid)throw new ArgumentException("Invalid dispatch policy.",nameof(policy));OwnScope();
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async()=>
        {
            db.ChangeTracker.Clear();
            try
            {
                await using var tx=await db.Database.BeginTransactionAsync(ct);await Fence(ct);
                var now=clock.GetUtcNow().UtcDateTime;
                var humans=await db.OutboxMessages.AsNoTracking().Where(o=>o.RecordedAtUtc<=now&&!db.OutboxDeliveries.Any(d=>d.HumanOutboxId==o.Id))
                    .OrderBy(o=>o.RecordedAtUtc).ThenBy(o=>o.Id).Take(policy.BatchSize).Select(o=>new{o.Id,o.RecordedAtUtc}).ToListAsync(ct);
                var systems=await db.SystemOutboxMessages.AsNoTracking().Where(o=>o.RecordedAtUtc<=now&&!db.OutboxDeliveries.Any(d=>d.SystemOutboxId==o.Id))
                    .OrderBy(o=>o.RecordedAtUtc).ThenBy(o=>o.Id).Take(policy.BatchSize).Select(o=>new{o.Id,o.RecordedAtUtc}).ToListAsync(ct);
                var added=humans.Select(o=>OutboxDelivery.Capture(Guid.NewGuid(),o.Id,null,Utc(o.RecordedAtUtc),now))
                    .Concat(systems.Select(o=>OutboxDelivery.Capture(Guid.NewGuid(),null,o.Id,Utc(o.RecordedAtUtc),now))).ToArray();
                db.OutboxDeliveries.AddRange(added);
                var ready=await db.OutboxDeliveries.Where(d=>d.State==OutboxDelivery.Pending&&d.AvailableAfterUtc<=now||
                    d.State==OutboxDelivery.Leased&&d.LeaseExpiresAtUtc<=now).OrderBy(d=>d.EventRecordedAtUtc).ThenBy(d=>d.Id)
                    .Take(policy.BatchSize).ToListAsync(ct);
                // Match SQL uniqueidentifier ordering when combining tracked old rows with new captures.
                var inspected=ready.Concat(added).OrderBy(d=>d.EventRecordedAtUtc).ThenBy(d=>new SqlGuid(d.Id)).Take(policy.BatchSize).ToArray();
                var claimed=new List<OutboxDelivery>();var dead=0;
                foreach(var item in inspected)
                {
                    if(item.TryLease(Guid.NewGuid(),now,TimeSpan.FromSeconds(policy.LeaseSeconds),policy.MaxAttempts))claimed.Add(item);
                    else if(item.State==OutboxDelivery.DeadLetter)dead++;
                }
                await db.SaveChangesAsync(ct);
                var humanIds=claimed.Where(x=>x.HumanOutboxId.HasValue).Select(x=>x.HumanOutboxId!.Value).ToArray();
                var systemIds=claimed.Where(x=>x.SystemOutboxId.HasValue).Select(x=>x.SystemOutboxId!.Value).ToArray();
                var humanFacts=await db.OutboxMessages.AsNoTracking().Where(x=>humanIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,ct);
                var systemFacts=await db.SystemOutboxMessages.AsNoTracking().Where(x=>systemIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,ct);
                var leases=new List<OutboxLease>();
                foreach(var delivery in claimed)
                {
                    OutboxEnvelope envelope;
                    if(delivery.HumanOutboxId is { } humanId)
                    {
                        var e=humanFacts[humanId];envelope=new(1,delivery.Id,"OutboxMessages",e.Id,e.Name,e.SchemaVersion,e.AggregateId,
                            e.ActorAccountId,null,null,null,Utc(e.OccurredAtUtc),Utc(e.RecordedAtUtc),e.PayloadJson);
                    }
                    else
                    {
                        var e=systemFacts[delivery.SystemOutboxId!.Value];envelope=new(1,delivery.Id,"SystemOutboxMessages",e.Id,e.Name,e.SchemaVersion,e.AggregateId,
                            null,e.Source,e.ResourceId,e.ResourceVersion,Utc(e.OccurredAtUtc),Utc(e.RecordedAtUtc),e.PayloadJson);
                    }
                    leases.Add(new(delivery.Id,delivery.LeaseToken!.Value,delivery.AttemptCount,envelope));
                }
                await tx.CommitAsync(ct);return new OutboxClaimBatch(inspected.Length,dead,leases);
            }
            finally{db.ChangeTracker.Clear();}
        });
    }
    public async Task<OutboxCompletionStatus> CompleteAsync(Guid deliveryId,Guid leaseToken,OutboxPublishResult result,OutboxDispatchPolicy policy,CancellationToken ct)
    {
        if(!policy.IsValid||!Enum.IsDefined(result.Disposition)||!Enum.IsDefined(result.Failure))throw new ArgumentException("Invalid dispatch outcome or policy.");OwnScope();
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async()=>
        {
            db.ChangeTracker.Clear();
            try
            {
                await using var tx=await db.Database.BeginTransactionAsync(ct);await Fence(ct);
                var delivery=await db.OutboxDeliveries.SingleOrDefaultAsync(x=>x.Id==deliveryId,ct);
                if(delivery is null){await tx.CommitAsync(ct);return OutboxCompletionStatus.LeaseLost;}
                var now=clock.GetUtcNow().UtcDateTime;
                if(!delivery.TryComplete(leaseToken,now,result.Disposition==OutboxPublishDisposition.Confirmed,
                    result.Disposition==OutboxPublishDisposition.PermanentFailure,result.Failure,policy.RetryDelay(delivery.AttemptCount),policy.MaxAttempts))
                {await tx.CommitAsync(ct);return OutboxCompletionStatus.LeaseLost;}
                await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
                return delivery.State switch{OutboxDelivery.Published=>OutboxCompletionStatus.Published,OutboxDelivery.DeadLetter=>OutboxCompletionStatus.DeadLettered,_=>OutboxCompletionStatus.RetryScheduled};
            }
            finally{db.ChangeTracker.Clear();}
        });
    }
}
