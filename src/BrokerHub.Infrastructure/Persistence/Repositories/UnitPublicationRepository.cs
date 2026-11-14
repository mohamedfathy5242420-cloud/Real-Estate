using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BrokerHub.Infrastructure.Persistence.Repositories;

internal sealed class UnitPublicationRepository(ApplicationDbContext db) : IUnitPublicationRepository
{
    public async Task<T> ExecuteAsync<T>(Guid unitId, Func<UnitPublicationContext, CancellationToken, Task<T>> command, CancellationToken ct)
    {
        (EntityEntry Entry, PropertyValues Expected)[] saved = [];
        return await db.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(db, async (context, token) =>
        {
            context.ChangeTracker.Clear(); saved = [];
            var owner = await (from u in context.Units.AsNoTracking()
                join p in context.Projects.AsNoTracking() on u.ProjectId equals p.Id
                where u.Id == unitId select new { p.Id, p.DeveloperId }).SingleOrDefaultAsync(token);
            if (owner is null) return await command(new(null, false, false, null, Guid.Empty), token);
            // Same key/order as agreement activation and active-parent saves. Lock persisted parents
            // even for hide, but do not require activity/coverage for removing an announcement.
            var developer = await context.Developers.FromSqlInterpolated($"SELECT * FROM [Catalog].[Developers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={owner.DeveloperId}").SingleOrDefaultAsync(token);
            var project = await context.Projects.FromSqlInterpolated($"SELECT * FROM [Catalog].[Projects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={owner.Id} AND [DeveloperId]={owner.DeveloperId}").SingleOrDefaultAsync(token);
            var unit = await context.Units.SingleOrDefaultAsync(u => u.Id == unitId, token);
            var sold = unit is not null && await context.Deals.AsNoTracking().AnyAsync(d => d.UnitId == unitId,token);
            var result = await command(new(unit, developer?.IsActive == true, project?.IsActive == true,
                project?.TimeZoneId, owner.DeveloperId,sold), token);
            saved = context.ChangeTracker.Entries().Select(e => (e, e.CurrentValues.Clone())).ToArray();
            return result;
        }, async (_, token) =>
        {
            // Commit acknowledgement loss: compare complete values including appended transition.
            if (!saved.Any(e => e.Entry.Entity is UnitPublicationTransition)) return false;
            foreach (var (entry, expected) in saved)
            {
                var persisted = await entry.GetDatabaseValuesAsync(token);
                if (persisted is null || expected.Properties.Any(p => !Equals(expected[p], persisted[p]))) return false;
            }
            return true;
        }, ct);
    }
    public async Task<Guid?> FindEligibleAgreementAsync(Guid developerId, Guid projectId, DateOnly date, CancellationToken ct)
    {
        // HOLDLOCK also protects the actual agreement/membership predicates through commit.
        // Active agreements cannot be edited by the current lifecycle; draft activation serializes
        // on Developer. Future lifecycle writers must retain the same lock protocol.
        return await db.Database.SqlQuery<Guid>($"""
            SELECT a.[Id] AS [Value] FROM [Catalog].[MarketingAgreements] a WITH (HOLDLOCK)
            WHERE a.[DeveloperId]={developerId} AND a.[Status]='Active'
              AND a.[StartsOn]<={date} AND a.[EndsOn]>={date}
              AND (a.[ScopeType]='AllDeveloperProjects' OR EXISTS (
                SELECT 1 FROM [Catalog].[AgreementProjects] p WITH (HOLDLOCK)
                WHERE p.[MarketingAgreementId]=a.[Id] AND p.[ProjectId]={projectId} AND p.[IsIncluded]=1))
            """).Select(id => (Guid?)id).SingleOrDefaultAsync(ct);
    }
    public async Task AddAsync(UnitPublicationTransition transition, CancellationToken ct) =>
        await db.UnitPublicationTransitions.AddAsync(transition, ct);
}
