using System.Data;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Notifications;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class UserNotificationQueries(ApplicationDbContext db):IUserNotificationQueries
{
    private Task<T> ReadAsync<T>(Func<Task<T>> read,CancellationToken ct)=>db.Database.CreateExecutionStrategy().ExecuteAsync(async()=>
    {
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var result=await read();await transaction.CommitAsync(ct);return result;
    });
    public Task<bool> CanAccessAsync(Guid actor,CancellationToken ct)=>new UserNotificationScope(db).CanAccessAsync(actor,ct);
    public Task<NotificationResult<UserNotificationPage>> ListAsync(Guid actor,NotificationListFilter filter,CancellationToken ct)=>ReadAsync<NotificationResult<UserNotificationPage>>(async()=>
    {
        var scope=new UserNotificationScope(db);if(!await scope.CanAccessAsync(actor,ct))return new(CommandStatus.Forbidden);
        if(!filter.IsValid)return new(CommandStatus.Invalid);
        var query=scope.Eligible(actor);
        if(filter.ReadStatus=="read")query=query.Where(n=>db.NotificationReadStates.Any(r=>r.NotificationId==n.Id));
        else if(filter.ReadStatus=="unread")query=query.Where(n=>!db.NotificationReadStates.Any(r=>r.NotificationId==n.Id));
        var count=await query.CountAsync(ct);
        var rows=await(from n in query.OrderByDescending(n=>n.CreatedAtUtc).ThenBy(n=>n.Id).Skip((filter.Page-1)*filter.PageSize).Take(filter.PageSize)
            join state in db.NotificationReadStates.AsNoTracking() on n.Id equals state.NotificationId into states
            from state in states.DefaultIfEmpty()
            orderby n.CreatedAtUtc descending,n.Id
            select new{Fact=n,ReadAtUtc=state==null?(DateTime?)null:state.ReadAtUtc}).ToArrayAsync(ct);
        return new(CommandStatus.Success,new(rows.Select(x=>x.Fact.ToUserNotification(x.ReadAtUtc)).ToArray(),count,filter.Page,filter.PageSize));
    },ct);
    public Task<NotificationResult<UserNotificationDto>> GetAsync(Guid actor,Guid id,CancellationToken ct)=>ReadAsync<NotificationResult<UserNotificationDto>>(async()=>
    {
        var scope=new UserNotificationScope(db);if(!await scope.CanAccessAsync(actor,ct))return new(CommandStatus.Forbidden);
        var row=await(from n in scope.Eligible(actor).Where(n=>n.Id==id)
            join state in db.NotificationReadStates.AsNoTracking() on n.Id equals state.NotificationId into states
            from state in states.DefaultIfEmpty() select new{Fact=n,ReadAtUtc=state==null?(DateTime?)null:state.ReadAtUtc}).SingleOrDefaultAsync(ct);
        return row is null?new(CommandStatus.NotFound):new(CommandStatus.Success,row.Fact.ToUserNotification(row.ReadAtUtc));
    },ct);
    public Task<NotificationResult<NotificationUnreadCountDto>> UnreadCountAsync(Guid actor,CancellationToken ct)=>ReadAsync<NotificationResult<NotificationUnreadCountDto>>(async()=>
    {
        var scope=new UserNotificationScope(db);if(!await scope.CanAccessAsync(actor,ct))return new(CommandStatus.Forbidden);
        return new(CommandStatus.Success,new(await scope.Eligible(actor).CountAsync(n=>!db.NotificationReadStates.Any(r=>r.NotificationId==n.Id),ct)));
    },ct);
}
