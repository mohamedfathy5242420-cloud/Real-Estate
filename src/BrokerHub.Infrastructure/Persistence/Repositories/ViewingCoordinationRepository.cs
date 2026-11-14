using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;
internal sealed class ViewingCoordinationRepository(ApplicationDbContext db) : IViewingCoordinationRepository
{
    public Task<Viewing?> LockAsync(Guid customerId, Guid id, CancellationToken ct)
    {
        RequireTransaction();
        return db.Viewings.FromSqlInterpolated($"""
            SELECT * FROM [Crm].[Viewings] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id} AND [CustomerId]={customerId}
            """).SingleOrDefaultAsync(ct);
    }
    public async Task<Guid?> LockResponsibleAccountAsync(Guid employeeId, CancellationToken ct)
    {
        RequireTransaction();
        var account = await db.Employees.AsNoTracking().Where(e => e.Id == employeeId).Select(e => (Guid?)e.AccountId).SingleOrDefaultAsync(ct);
        if (account is null) return null;
        // Before Unit/Employee resource locks: protect current Identity eligibility, not profile activity.
        var count = await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM [Identity].[Accounts] a WITH (HOLDLOCK)
            JOIN [Identity].[AccountRoles] ar WITH (HOLDLOCK) ON ar.[UserId]=a.[Id]
            JOIN [Identity].[Roles] r WITH (HOLDLOCK) ON r.[Id]=ar.[RoleId]
            WHERE a.[Id]={account.Value} AND a.[IsActive]=1 AND a.[EmailConfirmed]=1 AND r.[NormalizedName]=N'SALESEMPLOYEE'
            """).SingleAsync(ct);
        return count > 0 ? account : null;
    }
    public async Task<bool> LockResourcesAsync(Guid unitId, Guid employeeId, Guid accountId, CancellationToken ct)
    {
        RequireTransaction();
        if (await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM [Catalog].[Units] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={unitId}
            """).SingleAsync(ct) != 1) return false;
        return await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM [Crm].[Employees] WITH (UPDLOCK,HOLDLOCK)
            WHERE [Id]={employeeId} AND [AccountId]={accountId}
            """).SingleAsync(ct) == 1;
    }
    public async Task<bool> ConflictsAsync(Guid unitId, Guid employeeId, DateTime startUtc, DateTime endUtc, CancellationToken ct)
    {
        RequireTransaction();
        // Filtered covering indexes never visit locked pending contender roots.
        return await db.Database.SqlQuery<int>($"""
            SELECT CONVERT(int,CASE WHEN EXISTS(
                SELECT 1 FROM [Crm].[Viewings] WITH (INDEX([IX_Viewings_ConfirmedUnitInterval]))
                WHERE [Status]=N'Confirmed' AND [UnitId]={unitId} AND [ConfirmedStartUtc]<{endUtc} AND {startUtc}<[ConfirmedEndUtc])
              OR EXISTS(
                SELECT 1 FROM [Crm].[Viewings] WITH (INDEX([IX_Viewings_ConfirmedEmployeeInterval]))
                WHERE [Status]=N'Confirmed' AND [ConfirmedEmployeeId]={employeeId} AND [ConfirmedStartUtc]<{endUtc} AND {startUtc}<[ConfirmedEndUtc])
              THEN 1 ELSE 0 END) AS [Value]
            """).SingleAsync(ct) == 1;
    }
    private void RequireTransaction()
    { if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Viewing coordination requires the command transaction."); }
}
