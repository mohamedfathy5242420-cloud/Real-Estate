using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;
internal sealed class CrmFollowUpRepository(ApplicationDbContext db) : ICrmFollowUpRepository
{
    public async Task<bool> LockManagerAsync(Guid actor, CancellationToken ct)
    {
        RequireTransaction();
        return await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM [Identity].[Accounts] AS a WITH (HOLDLOCK)
            JOIN [Identity].[AccountRoles] AS ar WITH (HOLDLOCK) ON ar.[UserId]=a.[Id]
            JOIN [Identity].[Roles] AS r WITH (HOLDLOCK) ON r.[Id]=ar.[RoleId]
            WHERE a.[Id]={actor} AND a.[IsActive]=1 AND a.[EmailConfirmed]=1 AND r.[NormalizedName]=N'SALESMANAGER'
            """).SingleAsync(ct) > 0;
    }
    public Task<SalesNote?> LockNoteAsync(Guid customerId, Guid noteId, CancellationToken ct)
    {
        RequireTransaction();
        return db.SalesNotes.FromSqlInterpolated($"SELECT * FROM [Crm].[SalesNotes] WITH (HOLDLOCK) WHERE [Id]={noteId} AND [CustomerId]={customerId}").AsNoTracking().SingleOrDefaultAsync(ct);
    }
    public Task<Guid?> FindAuthorEmployeeAsync(Guid actor, CancellationToken ct)
    { RequireTransaction(); return db.Employees.AsNoTracking().Where(e => e.AccountId == actor).Select(e => (Guid?)e.Id).SingleOrDefaultAsync(ct); }
    public void AddCustomer(Customer c) { RequireTransaction(); db.Customers.Add(c); }
    public void AddNote(SalesNote n) { RequireTransaction(); db.SalesNotes.Add(n); }
    public void AddNeedsChange(CustomerNeedsChange h) { RequireTransaction(); db.CustomerNeedsChanges.Add(h); }
    private void RequireTransaction()
    { if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Follow-up writes require the command transaction."); }
}
