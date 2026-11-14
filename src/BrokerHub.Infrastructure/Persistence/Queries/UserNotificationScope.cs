using BrokerHub.Application.DTOs.Messaging;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class UserNotificationScope(ApplicationDbContext db)
{
    private static string[] Names(NotificationAudience audience)=>NotificationEventCatalog.Events.Values
        .Where(x=>x.Audience.HasFlag(audience)&&UserNotificationMappings.Templates.ContainsKey(x.Name)).Select(x=>x.Name).ToArray();
    private static readonly string[] Known=Names(NotificationAudience.Manager),Owners=Names(NotificationAudience.CustomerOwner),Employees=Names(NotificationAudience.AssignedEmployee);
    private IQueryable<Guid> AccountsIn(string role)=>from ar in db.UserRoles join r in db.Roles on ar.RoleId equals r.Id where r.NormalizedName==role select ar.UserId;
    public Task<bool> CanAccessAsync(Guid actor,CancellationToken ct)
    {
        var owners=AccountsIn("CUSTOMER");var employees=AccountsIn("SALESEMPLOYEE");var managers=AccountsIn("SALESMANAGER");
        return db.Users.AsNoTracking().AnyAsync(a=>a.Id==actor&&a.IsActive&&a.EmailConfirmed&&
            (owners.Contains(actor)||employees.Contains(actor)||managers.Contains(actor)),ct);
    }
    public IQueryable<Notification> Eligible(Guid actor)
    {
        var owners=AccountsIn("CUSTOMER");var employees=AccountsIn("SALESEMPLOYEE");var managers=AccountsIn("SALESMANAGER");
        return db.Notifications.AsNoTracking().Where(n=>n.RecipientAccountId==actor&&Known.Contains(n.EventName)&&
            db.Users.Any(a=>a.Id==actor&&a.IsActive&&a.EmailConfirmed)&&
            (managers.Contains(actor)||owners.Contains(actor)&&Owners.Contains(n.EventName)&&db.Customers.Any(c=>c.Id==n.CustomerId&&c.AccountId==actor)||
                employees.Contains(actor)&&Employees.Contains(n.EventName)&&db.Customers.Any(c=>c.Id==n.CustomerId&&
                    db.Employees.Any(e=>e.Id==c.AssignedEmployeeId&&e.AccountId==actor))));
    }
}
