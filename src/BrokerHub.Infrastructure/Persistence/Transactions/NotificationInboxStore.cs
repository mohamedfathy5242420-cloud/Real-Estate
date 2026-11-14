using System.Data;
using System.Security.Cryptography;
using System.Text;
using BrokerHub.Application.DTOs.Messaging;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Persistence.Models;
using BrokerHub.Infrastructure.Persistence.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BrokerHub.Infrastructure.Persistence.Transactions;

internal sealed class NotificationInboxStore(ApplicationDbContext db,TimeProvider clock):INotificationInboxStore
{
    public async Task<NotificationConsumeResult> ConsumeAsync(ValidatedNotificationEnvelope message,CancellationToken ct)
    {
        if(db.Database.CurrentTransaction is not null||db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Inbox processing requires its own clean transaction scope.");
        if(!message.Envelope.IsValid()||message.BodyHash is not {Length:64}||
            !message.BodyHash.All(c=>char.IsAsciiDigit(c)||c is >= 'A' and <= 'F'))return new(NotificationConsumeStatus.Rejected);
        List<(EntityEntry Entry,PropertyValues Expected)> staged=[];InboxMessage? committed=null;
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(db,async(context,token)=>
            {
                context.ChangeTracker.Clear();staged=[];committed=null;
                var queries=new NotificationSourceQueries(context);var source=await queries.ReadAsync(message,token);
                if(source is null)return new NotificationConsumeResult(NotificationConsumeStatus.Rejected);
                var consumer=new SqlParameter("consumer",SqlDbType.VarChar,64){Value=InboxMessage.NotificationConsumer};
                var existing=await context.InboxMessages.FromSqlInterpolated($"""
                    SELECT * FROM [Reliability].[InboxMessages] WITH (UPDLOCK,HOLDLOCK,INDEX([UX_InboxMessages_ConsumerMessage]))
                    WHERE [ConsumerName]={consumer} AND [MessageId]={source.Envelope.MessageId}
                    """).AsNoTracking().SingleOrDefaultAsync(token);
                if(existing is not null)
                {
                    if(!Matches(existing,source))return new NotificationConsumeResult(NotificationConsumeStatus.Rejected);
                    if(!await VerifyOutcome(context,existing,source,token))throw new InvalidOperationException("Committed inbox outcome is incomplete.");
                    return new NotificationConsumeResult(NotificationConsumeStatus.Duplicate,existing.NotificationCount);
                }
                var recipients=await queries.RecipientsAsync(source,token);var now=clock.GetUtcNow().UtcDateTime;
                committed=InboxMessage.Commit(Guid.NewGuid(),source.Envelope.MessageId,source.BodyHash,source.Envelope.SourceStore,
                    source.Envelope.SourceEventId,source.Envelope.Name,source.Envelope.EventSchemaVersion,recipients.Length,Digest(recipients),
                    source.Envelope.OccurredAtUtc,source.Envelope.RecordedAtUtc,now);
                context.InboxMessages.Add(committed);
                foreach(var recipient in recipients)context.Notifications.Add(Notification.Create(Guid.NewGuid(),committed,recipient,
                    source.CustomerId,source.Definition.ResourceKind,source.ResourceId,source.ParentResourceId,source.RelatedResourceId));
                context.NotificationProcessingAudits.Add(NotificationProcessingAudit.Record(committed,source.Envelope.ActorAccountId,source.Envelope.SystemSource));
                context.ChangeTracker.DetectChanges();
                var changes=context.ChangeTracker.Entries().Where(e=>e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
                if(changes.Count!=recipients.Length+2||changes.Any(e=>e.State!=EntityState.Added||e.Entity is not (InboxMessage or Notification or NotificationProcessingAudit)))
                    throw new InvalidOperationException("Inbox can stage only its exact notification and audit outcome.");
                staged=changes.Select(e=>(e,e.CurrentValues.Clone())).ToList();
                await context.SaveChangesAsync(acceptAllChangesOnSuccess:false,token);
                return new NotificationConsumeResult(NotificationConsumeStatus.Applied,recipients.Length);
            },async(context,token)=>
            {
                if(committed is null)return true;
                foreach(var(entry,expected)in staged)
                {
                    var actual=await entry.GetDatabaseValuesAsync(token);
                    if(actual is null||expected.Properties.Any(p=>!Equals(expected[p],actual[p])))return false;
                }
                return staged.Count==committed.NotificationCount+2&&
                    await context.Notifications.CountAsync(x=>x.InboxMessageId==committed.Id,token)==committed.NotificationCount&&
                    await context.NotificationProcessingAudits.CountAsync(x=>x.InboxMessageId==committed.Id,token)==1;
            },ct);
        }
        finally{db.ChangeTracker.Clear();}
    }
    private static bool Matches(InboxMessage inbox,NotificationSourceFacts source)=>
        inbox.ConsumerName==InboxMessage.NotificationConsumer&&inbox.MessageId==source.Envelope.MessageId&&inbox.BodyHash==source.BodyHash&&
        inbox.SourceStore==source.Envelope.SourceStore&&inbox.SourceEventId==source.Envelope.SourceEventId&&inbox.EventName==source.Envelope.Name&&
        inbox.EventSchemaVersion==source.Envelope.EventSchemaVersion&&inbox.OccurredAtUtc==source.Envelope.OccurredAtUtc&&
        inbox.SourceRecordedAtUtc==source.Envelope.RecordedAtUtc;
    private static async Task<bool> VerifyOutcome(ApplicationDbContext context,InboxMessage inbox,NotificationSourceFacts source,CancellationToken ct)
    {
        var audit=await context.NotificationProcessingAudits.AsNoTracking().SingleOrDefaultAsync(x=>x.InboxMessageId==inbox.Id,ct);
        if(audit is null||audit.MessageId!=inbox.MessageId||audit.ConsumerName!=inbox.ConsumerName||audit.SourceStore!=inbox.SourceStore||
            audit.SourceEventId!=inbox.SourceEventId||audit.SourceActorAccountId!=source.Envelope.ActorAccountId||audit.SystemSource!=source.Envelope.SystemSource||
            audit.EventName!=inbox.EventName||audit.EventSchemaVersion!=inbox.EventSchemaVersion||audit.BodyHash!=inbox.BodyHash||
            audit.NotificationCount!=inbox.NotificationCount||audit.RecipientDigest!=inbox.RecipientDigest||
            audit.OccurredAtUtc!=inbox.OccurredAtUtc||audit.ProcessedAtUtc!=inbox.ProcessedAtUtc)return false;
        var notifications=await context.Notifications.AsNoTracking().Where(x=>x.InboxMessageId==inbox.Id).ToArrayAsync(ct);
        return notifications.Length==inbox.NotificationCount&&Digest(notifications.Select(x=>x.RecipientAccountId))==inbox.RecipientDigest&&
            notifications.All(x=>x.EventName==inbox.EventName&&x.CustomerId==source.CustomerId&&x.ResourceKind==source.Definition.ResourceKind&&
                x.ResourceId==source.ResourceId&&x.ParentResourceId==source.ParentResourceId&&x.RelatedResourceId==source.RelatedResourceId&&
                x.OccurredAtUtc==inbox.OccurredAtUtc&&x.CreatedAtUtc==inbox.ProcessedAtUtc);
    }
    private static string Digest(IEnumerable<Guid> recipients)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join('\n',recipients.Select(x=>x.ToString("N")).Order(StringComparer.Ordinal)))));
}
