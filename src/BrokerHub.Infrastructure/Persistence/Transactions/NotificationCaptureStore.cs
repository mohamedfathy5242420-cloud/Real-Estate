using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Notifications;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Storage;
using BrokerHub.Application.Mappings;
using BrokerHub.Application.Serialization;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BrokerHub.Infrastructure.Persistence.Transactions;

internal sealed class NotificationCaptureStore(ApplicationDbContext db,ILocalNotificationArtifactStore artifacts,TimeProvider clock):INotificationCaptureStore
{
    public async Task<LocalNotificationCaptureResult> CaptureAsync(Guid recipientAccountId,Guid notificationId,CancellationToken ct)
    {
        if(db.Database.CurrentTransaction is not null||db.ChangeTracker.HasChanges())throw new InvalidOperationException("Capture needs its own clean transaction.");
        EntityEntry? staged=null;PropertyValues? expected=null;LocalNotificationArtifact? artifact=null;
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(db,async(context,token)=>
            {
                context.ChangeTracker.Clear();staged=null;expected=null;artifact=null;
                var access=await new NotificationMutationScope(context).LockAsync(recipientAccountId,notificationId,token);
                if(access.Status!=CommandStatus.Success)return new LocalNotificationCaptureResult(access.Status==CommandStatus.Forbidden?LocalNotificationCaptureStatus.Forbidden:LocalNotificationCaptureStatus.NotFound);
                var notification=access.Notification??throw new InvalidOperationException("Authorized capture notification required.");
                var previous=await context.NotificationEmailCaptures.FromSqlInterpolated($"""
                    SELECT * FROM [Messaging].[NotificationEmailCaptures] WITH (UPDLOCK,HOLDLOCK) WHERE [NotificationId]={notificationId}
                    """).AsNoTracking().SingleOrDefaultAsync(token);
                artifact=LocalNotificationCaptureContent.Create(notification.ToUserNotification(null),recipientAccountId);
                if(previous is not null&&previous.ContentHash!=artifact.ContentHash)return new LocalNotificationCaptureResult(LocalNotificationCaptureStatus.Conflict);
                await artifacts.EnsureAsync(artifact,token);
                if(previous is not null)return new LocalNotificationCaptureResult(LocalNotificationCaptureStatus.Success,notificationId,UserNotificationMappings.Utc(previous.CapturedAtUtc),previous.ContentHash);
                var receipt=NotificationEmailCapture.Record(notification,artifact.ContentHash,clock.GetUtcNow().UtcDateTime);context.NotificationEmailCaptures.Add(receipt);
                context.ChangeTracker.DetectChanges();var changes=context.ChangeTracker.Entries().Where(e=>e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
                if(changes.Length!=1||changes[0].State!=EntityState.Added||changes[0].Entity!=receipt)throw new InvalidOperationException("Capture can stage only its own receipt.");
                staged=changes[0];expected=staged.CurrentValues.Clone();await context.SaveChangesAsync(acceptAllChangesOnSuccess:false,token);
                return new LocalNotificationCaptureResult(LocalNotificationCaptureStatus.Success,notificationId,UserNotificationMappings.Utc(receipt.CapturedAtUtc),receipt.ContentHash);
            },async(_,token)=>
            {
                if(staged is null||expected is null)return true;var actual=await staged.GetDatabaseValuesAsync(token);
                return actual is not null&&expected.Properties.All(p=>Equals(expected[p],actual[p]))&&artifact is not null&&await artifacts.VerifyAsync(artifact,token);
            },ct);
        }
        catch(LocalNotificationArtifactConflictException){return new(LocalNotificationCaptureStatus.Conflict);}
        finally{db.ChangeTracker.Clear();}
    }
}
