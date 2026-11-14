using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Notifications;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BrokerHub.Infrastructure.Persistence.Transactions;

internal sealed class NotificationReadStore(ApplicationDbContext db,TimeProvider clock):INotificationReadStore
{
    public async Task<NotificationResult<NotificationReadReply>> MarkReadAsync(Guid actor,Guid notificationId,CancellationToken ct)
    {
        if(db.Database.CurrentTransaction is not null||db.ChangeTracker.HasChanges())throw new InvalidOperationException("Notification read-state needs a clean owned transaction.");
        EntityEntry? staged=null;PropertyValues? expected=null;
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(db,async(context,token)=>
            {
                context.ChangeTracker.Clear();staged=null;expected=null;
                var access=await new NotificationMutationScope(context).LockAsync(actor,notificationId,token);
                if(access.Status!=CommandStatus.Success)return new NotificationResult<NotificationReadReply>(access.Status);
                // Current role/resource guard deliberately precedes any existing read-state replay.
                var previous=await context.NotificationReadStates.FromSqlInterpolated($"""
                    SELECT * FROM [Messaging].[NotificationReadStates] WITH (UPDLOCK,HOLDLOCK) WHERE [NotificationId]={notificationId}
                    """).AsNoTracking().SingleOrDefaultAsync(token);
                if(previous is not null)return new NotificationResult<NotificationReadReply>(CommandStatus.Success,new(notificationId,UserNotificationMappings.Utc(previous.ReadAtUtc)));
                var state=NotificationReadState.Record(access.Notification!,clock.GetUtcNow().UtcDateTime);context.NotificationReadStates.Add(state);
                context.ChangeTracker.DetectChanges();var changes=context.ChangeTracker.Entries().Where(e=>e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
                if(changes.Length!=1||changes[0].State!=EntityState.Added||changes[0].Entity!=state)throw new InvalidOperationException("Read-state can stage only its own first-read fact.");
                staged=changes[0];expected=staged.CurrentValues.Clone();await context.SaveChangesAsync(acceptAllChangesOnSuccess:false,token);
                return new NotificationResult<NotificationReadReply>(CommandStatus.Success,new(notificationId,UserNotificationMappings.Utc(state.ReadAtUtc)));
            },async(_,token)=>
            {
                if(staged is null||expected is null)return true;var actual=await staged.GetDatabaseValuesAsync(token);
                return actual is not null&&expected.Properties.All(p=>Equals(expected[p],actual[p]));
            },ct);
        }
        finally{db.ChangeTracker.Clear();}
    }
}
