using BrokerHub.Application.Authorization;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using BrokerHub.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;

internal sealed class CustomerLinkRepository(ApplicationDbContext db) : ICustomerLinkRepository
{
    public Task<CustomerLinkRequest?> GetTrackedAsync(Guid id, CancellationToken ct) =>
        db.CustomerLinkRequests.Include(r => r.History).SingleOrDefaultAsync(r => r.Id == id, ct);
    public Task<Customer?> GetCustomerTrackedAsync(Guid id, CancellationToken ct) => db.Customers.SingleOrDefaultAsync(c => c.Id == id, ct);
    public Task<bool> HasProfileAsync(Guid accountId, CancellationToken ct) => db.Customers.AnyAsync(c => c.AccountId == accountId, ct);
    public void Add(CustomerLinkRequest request) => db.CustomerLinkRequests.Add(request);

    public async Task CommitAsync(CustomerLinkRequest request, Customer? target, CancellationToken ct)
    {
        db.ChangeTracker.DetectChanges();
        var pending = db.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e => (Entry: e, Expected: e.CurrentValues.Clone())).ToArray();
        var submitting = request.Status == CustomerLinkRequestStatus.Pending;
        var approving = request.Status == CustomerLinkRequestStatus.Approved;
        var actor = submitting ? request.AccountId : request.DecidedByAccountId!.Value;
        try
        {
            await db.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(db, async (context, token) =>
            {
                // Consistent identity order. UPDLOCK/HOLDLOCK also protects eligibility with RCSI enabled.
                foreach (var id in (approving ? new[] { actor, request.AccountId } : new[] { actor }).Distinct().Order())
                {
                    var active = await context.Database.SqlQuery<int>($"""
                        SELECT CONVERT(int, CASE WHEN [IsActive]=1 AND [EmailConfirmed]=1 THEN 1 ELSE 0 END) AS [Value]
                        FROM [Identity].[Accounts] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}
                        """).SingleOrDefaultAsync(token);
                    if (active != 1) throw new CustomerLinkEligibilityException(id == actor);
                }
                await RequireRole(actor, submitting ? AppRoles.Customer : AppRoles.SalesManager, true, token);
                if (approving) await RequireRole(request.AccountId, AppRoles.Customer, false, token);
                if (submitting || approving)
                {
                    // Lock the account's unique-index range, including a missing row. A concurrent own
                    // profile create must finish before this check, or wait until this transaction ends.
                    var linked = await context.Database.SqlQuery<int>($"""
                        SELECT COUNT(*) AS [Value] FROM [Crm].[Customers] WITH (UPDLOCK,HOLDLOCK,INDEX([UX_Customers_AccountId]))
                        WHERE [AccountId]={request.AccountId}
                        """).SingleAsync(token);
                    if (linked != 0) throw new CustomerLinkEligibilityException(false);
                }
                if (target is not null)
                {
                    var eligible = await context.Database.SqlQuery<int>($"""
                        SELECT COUNT(*) AS [Value] FROM [Crm].[Customers] WITH (UPDLOCK,HOLDLOCK)
                        WHERE [Id]={target.Id} AND [AccountId] IS NULL AND [Version]={target.Version - 1}
                        """).SingleAsync(token);
                    if (eligible != 1) throw new CustomerLinkEligibilityException(false);
                }
                await context.SaveChangesAsync(acceptAllChangesOnSuccess: false, token);
                return true;
            }, async (_, token) =>
            {
                // Resolve uncertain commit against every exact persisted effect, not a Version alone.
                foreach (var (entry, expected) in pending)
                {
                    var persisted = await entry.GetDatabaseValuesAsync(token);
                    if (persisted is null || expected.Properties.Any(p => !Equals(expected[p], persisted[p]))) return false;
                }
                return pending.Length > 0;
            }, ct);
            db.ChangeTracker.AcceptAllChanges();
        }
        catch (DbUpdateConcurrencyException ex) { throw new OptimisticConcurrencyException("Link request changed before commit.", ex); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { throw new UniqueConstraintViolationException("Account or pending request is already linked.", ex); }
    }
    private async Task RequireRole(Guid accountId, string role, bool actor, CancellationToken ct)
    {
        var normalizedRole = role.ToUpperInvariant();
        var allowed = await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM [Identity].[AccountRoles] AS ar WITH (UPDLOCK,HOLDLOCK)
            INNER JOIN [Identity].[Roles] AS r WITH (HOLDLOCK) ON r.[Id]=ar.[RoleId]
            WHERE ar.[UserId]={accountId} AND r.[NormalizedName]={normalizedRole}
            """).SingleAsync(ct);
        if (allowed != 1) throw new CustomerLinkEligibilityException(actor);
    }
}
