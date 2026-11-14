using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;

internal sealed class CrmCustomerRepository(ApplicationDbContext db) : ICrmCustomerRepository
{
    public async Task<CustomerSalesLock> LockForSalesAsync(Guid customerId, Guid actor, bool managerOnly, CancellationToken ct)
    {
        RequireTransaction();
        var manager = await HasRole(actor, AppRoles.SalesManager, ct);
        if (!manager && (managerOnly || !await HasRole(actor, AppRoles.SalesEmployee, ct))) return new(CommandPermission.Forbidden);
        var customer = await db.Customers.FromSqlInterpolated($"""
            SELECT * FROM [Crm].[Customers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={customerId}
            """).SingleOrDefaultAsync(ct);
        if (customer is null) return new(CommandPermission.NotFound);
        if (!manager && !await db.Employees.AnyAsync(e => e.Id == customer.AssignedEmployeeId && e.AccountId == actor, ct))
            return new(CommandPermission.NotFound);
        return new(CommandPermission.Allowed, customer);
    }
    public async Task<bool> LockEligibleAssigneeAsync(Guid employeeId, CancellationToken ct)
    {
        RequireTransaction();
        // AccountId is immutable. Resolve it first, then protect Account -> Profile order, matching
        // existing staff-profile mutations. Shared locks protect read eligibility through commit.
        var account = await db.Employees.AsNoTracking().Where(e => e.Id == employeeId).Select(e => (Guid?)e.AccountId).SingleOrDefaultAsync(ct);
        if (account is null) return false;
        var active = await db.Database.SqlQuery<int>($"""
            SELECT CONVERT(int,CASE WHEN [IsActive]=1 AND [EmailConfirmed]=1 THEN 1 ELSE 0 END) AS [Value]
            FROM [Identity].[Accounts] WITH (HOLDLOCK) WHERE [Id]={account.Value}
            """).SingleOrDefaultAsync(ct);
        if (active != 1 || !await HasRole(account.Value, AppRoles.SalesEmployee, ct)) return false;
        return await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM [Crm].[Employees] WITH (HOLDLOCK)
            WHERE [Id]={employeeId} AND [AccountId]={account.Value} AND [IsActive]=1
            """).SingleAsync(ct) == 1;
    }
    public void AddAssignment(CustomerAssignment assignment) => db.CustomerAssignments.Add(assignment);
    private async Task<bool> HasRole(Guid actor, string role, CancellationToken ct)
    {
        var normalized = role.ToUpperInvariant();
        return await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM [Identity].[Accounts] AS a WITH (HOLDLOCK)
            INNER JOIN [Identity].[AccountRoles] AS ar WITH (HOLDLOCK) ON ar.[UserId]=a.[Id]
            INNER JOIN [Identity].[Roles] AS r WITH (HOLDLOCK) ON r.[Id]=ar.[RoleId]
            WHERE a.[Id]={actor} AND a.[IsActive]=1 AND a.[EmailConfirmed]=1 AND r.[NormalizedName]={normalized}
            """).SingleAsync(ct) > 0;
    }
    private void RequireTransaction()
    { if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("CRM mutation access requires the command transaction."); }
}
