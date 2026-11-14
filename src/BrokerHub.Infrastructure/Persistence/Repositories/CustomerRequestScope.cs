using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Requests;
using BrokerHub.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;
internal sealed class CustomerRequestScope(ApplicationDbContext db) : ICustomerRequestScope
{
    public async Task<CustomerRequestLock> LockOwnedAsync(Guid actor, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Owned request scope requires the command transaction.");
        var allowed = await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM [Identity].[Accounts] a WITH (HOLDLOCK)
            JOIN [Identity].[AccountRoles] ar WITH (HOLDLOCK) ON ar.[UserId]=a.[Id]
            JOIN [Identity].[Roles] r WITH (HOLDLOCK) ON r.[Id]=ar.[RoleId]
            WHERE a.[Id]={actor} AND a.[IsActive]=1 AND a.[EmailConfirmed]=1 AND r.[NormalizedName]=N'CUSTOMER'
            """).SingleAsync(ct) > 0;
        if (!allowed) return new(CommandPermission.Forbidden);
        var customer = await db.Customers.FromSqlInterpolated($"""
            SELECT * FROM [Crm].[Customers] WITH (UPDLOCK,HOLDLOCK,INDEX([UX_Customers_AccountId]))
            WHERE [AccountId]={actor} AND [AccountId] IS NOT NULL
            """).SingleOrDefaultAsync(ct);
        return customer is null ? new(CommandPermission.NotFound) : new(CommandPermission.Allowed, customer);
    }
}
