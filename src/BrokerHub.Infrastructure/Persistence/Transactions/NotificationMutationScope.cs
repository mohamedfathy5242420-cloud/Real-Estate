using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Messaging;
using BrokerHub.Application.Mappings;
using BrokerHub.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Transactions;

internal sealed class NotificationMutationScope(ApplicationDbContext db)
{
    public async Task<NotificationAccessLock> LockAsync(Guid actor,Guid id,CancellationToken ct)
    {
        if(db.Database.CurrentTransaction is null)throw new InvalidOperationException("Notification permission guard requires its own transaction.");
        var active=await db.Database.SqlQuery<int>($"""
            SELECT CONVERT(int,CASE WHEN [IsActive]=1 AND [EmailConfirmed]=1 THEN 1 ELSE 0 END) AS [Value]
            FROM [Identity].[Accounts] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={actor}
            """).SingleOrDefaultAsync(ct);
        if(active!=1)return new(CommandStatus.Forbidden);
        var roles=await db.Database.SqlQuery<string>($"""
            SELECT r.[NormalizedName] AS [Value] FROM [Identity].[AccountRoles] ar WITH (HOLDLOCK)
            JOIN [Identity].[Roles] r WITH (HOLDLOCK) ON r.[Id]=ar.[RoleId] WHERE ar.[UserId]={actor}
            """).ToArrayAsync(ct);
        var manager=roles.Contains("SALESMANAGER");var owner=roles.Contains("CUSTOMER");var employee=roles.Contains("SALESEMPLOYEE");
        if(!manager&&!owner&&!employee)return new(CommandStatus.Forbidden);
        var candidate=await db.Notifications.AsNoTracking().SingleOrDefaultAsync(n=>n.Id==id&&n.RecipientAccountId==actor,ct);
        if(candidate is null)return new(CommandStatus.NotFound);
        var definition=NotificationEventCatalog.Find(candidate.EventName);
        if(definition is null||definition.Audience==NotificationAudience.None||!UserNotificationMappings.Templates.ContainsKey(candidate.EventName))return new(CommandStatus.NotFound);
        BrokerHub.Domain.Entities.Customer? customer=null;
        if(candidate.CustomerId is { } customerId)customer=await db.Customers.FromSqlInterpolated($"""
            SELECT * FROM [Crm].[Customers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={customerId}
            """).AsNoTracking().SingleOrDefaultAsync(ct);
        var allowed=manager&&definition.Audience.HasFlag(NotificationAudience.Manager)||
            owner&&definition.Audience.HasFlag(NotificationAudience.CustomerOwner)&&customer?.AccountId==actor;
        if(!allowed&&employee&&definition.Audience.HasFlag(NotificationAudience.AssignedEmployee)&&customer?.AssignedEmployeeId is { } employeeId)
            allowed=await db.Employees.FromSqlInterpolated($"""
                SELECT * FROM [Crm].[Employees] WITH (HOLDLOCK) WHERE [Id]={employeeId}
                """).AsNoTracking().AnyAsync(e=>e.AccountId==actor,ct);
        if(!allowed)return new(CommandStatus.NotFound);
        var fact=await db.Notifications.FromSqlInterpolated($"""
            SELECT * FROM [Messaging].[Notifications] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id} AND [RecipientAccountId]={actor}
            """).AsNoTracking().SingleOrDefaultAsync(ct);
        return fact is null||fact.CustomerId!=candidate.CustomerId||fact.EventName!=candidate.EventName?
            new(CommandStatus.NotFound):new(CommandStatus.Success,fact);
    }
}
