using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesWithActiveDeveloperAsync(
        Guid developerId,
        CancellationToken cancellationToken)
        => await SaveProtectedAsync(developerId, projectId: null, cancellationToken);

    public async Task SaveChangesWithActiveCatalogParentsAsync(
        Guid projectId, Guid developerId, CancellationToken cancellationToken)
        => await SaveProtectedAsync(developerId, projectId, cancellationToken);

    private async Task SaveProtectedAsync(Guid developerId, Guid? projectId, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.DetectChanges();
        var pending = dbContext.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => (Entry: entry, Expected: entry.CurrentValues.Clone()))
            .ToArray();

        // The retry strategy owns the whole transaction, not just SaveChanges. Keep entry state
        // until commit is confirmed so a rolled-back attempt can be replayed safely.
        await dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(
            dbContext,
            async (context, token) =>
            {
                // Parameterized SQL deliberately bypasses the tracked Developer snapshot.
                // UPDLOCK/HOLDLOCK prevents a concurrent deactivation until this transaction ends,
                // including when READ_COMMITTED_SNAPSHOT is enabled.
                var active = await context.Database.SqlQuery<int>($"""
                    SELECT CONVERT(int, [IsActive]) AS [Value]
                    FROM [Catalog].[Developers] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [Id] = {developerId}
                    """).SingleOrDefaultAsync(token);
                if (active != 1)
                {
                    if (projectId.HasValue) throw new CatalogParentNotActiveException("DeveloperId");
                    throw new DeveloperNotActiveException();
                }

                if (projectId.HasValue)
                {
                    // Developer -> Project lock order matches Project create/reactivate. Read the
                    // persisted relationship/activity, not the stale pre-check/tracked parent.
                    var projectActive = await context.Database.SqlQuery<int>($"""
                        SELECT CONVERT(int, [IsActive]) AS [Value]
                        FROM [Catalog].[Projects] WITH (UPDLOCK, HOLDLOCK)
                        WHERE [Id] = {projectId.Value} AND [DeveloperId] = {developerId}
                        """).SingleOrDefaultAsync(token);
                    if (projectActive != 1) throw new CatalogParentNotActiveException("ProjectId");
                }

                await SaveCoreAsync(acceptAllChangesOnSuccess: false, token);
                return true;
            },
            async (_, token) =>
            {
                // Resolve an uncertain commit using the exact persisted values, not Version alone.
                foreach (var (entry, expected) in pending)
                {
                    var persisted = await entry.GetDatabaseValuesAsync(token);
                    if (persisted is null || expected.Properties.Any(property =>
                        !Equals(expected[property], persisted[property])))
                    {
                        return false;
                    }
                }

                return pending.Length > 0;
            },
            cancellationToken);

        dbContext.ChangeTracker.AcceptAllChanges();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await SaveCoreAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    private async Task SaveCoreAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new OptimisticConcurrencyException(
                "The record changed before the operation could be committed.",
                exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new UniqueConstraintViolationException(
                "A unique database constraint was violated.",
                exception);
        }
    }
}
