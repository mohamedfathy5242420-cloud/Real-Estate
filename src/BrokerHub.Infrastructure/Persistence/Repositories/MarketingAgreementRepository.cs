using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using BrokerHub.Domain.Enums;
using BrokerHub.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BrokerHub.Infrastructure.Persistence.Repositories;

internal sealed class MarketingAgreementRepository(ApplicationDbContext db) : IMarketingAgreementRepository
{
    private HashSet<Guid>? loadedRuleIds;
    private HashSet<Guid>? loadedProjectIds;
    private MarketingAgreement? loadedAgreement;
    public async Task<T> ExecuteActivationAsync<T>(Guid developerId, Func<CancellationToken, Task<T>> command, CancellationToken ct)
    {
        (EntityEntry Entry, PropertyValues Expected)[] saved = [];
        var result = await db.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(db, async (context, token) =>
        {
            // A retry owns a fresh tracked graph; never replay an already-activated in-memory object.
            context.ChangeTracker.Clear(); saved = [];
            var active = await context.Database.SqlQuery<int>($"""
                SELECT CONVERT(int, [IsActive]) AS [Value]
                FROM [Catalog].[Developers] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {developerId}
                """).SingleOrDefaultAsync(token);
            if (active != 1) throw new DeveloperNotActiveException();
            var value = await command(token);
            saved = context.ChangeTracker.Entries().Select(e => (e, e.CurrentValues.Clone())).ToArray();
            return value;
        }, async (_, token) =>
        {
            // Resolve acknowledgement loss using complete persisted values, including the revision.
            // A rolled-back attempt is replayed from fresh reads under the same Developer lock.
            foreach (var (entry, expected) in saved)
            {
                var persisted = await entry.GetDatabaseValuesAsync(token);
                if (persisted is null || expected.Properties.Any(p => !Equals(expected[p], persisted[p]))) return false;
            }
            return saved.Length > 0;
        }, ct);
        return result;
    }
    public Task<bool> ActiveOverlapExistsAsync(MarketingAgreement a, CancellationToken ct)
    {
        var selected = a.Projects.Where(p => p.IsIncluded).Select(p => p.ProjectId).ToArray();
        // Symbolic All coverage, not current-project expansion: protects zero-project and future-project cases.
        return db.MarketingAgreements.AsNoTracking().AnyAsync(other => other.Id != a.Id &&
            other.DeveloperId == a.DeveloperId && other.Status == "Active" &&
            other.StartsOn <= a.EndsOn && a.StartsOn <= other.EndsOn &&
            (a.ScopeType == AgreementScopeType.AllDeveloperProjects || other.ScopeType == AgreementScopeType.AllDeveloperProjects ||
             other.Projects.Any(p => p.IsIncluded && selected.Contains(p.ProjectId))), ct);
    }
    public async Task<MarketingAgreement?> GetTrackedByIdAsync(Guid id, CancellationToken ct)
    {
        loadedAgreement = await db.MarketingAgreements.Include(a => a.Projects).Include(a => a.Rules)
            .AsSingleQuery().SingleOrDefaultAsync(a => a.Id == id, ct);
        loadedRuleIds = loadedAgreement?.Rules.Select(r => r.Id).ToHashSet();
        loadedProjectIds = loadedAgreement?.Projects.Select(p => p.ProjectId).ToHashSet();
        return loadedAgreement;
    }
    public Task<bool> NumberExistsAsync(Guid developerId, string number, Guid? exceptId, CancellationToken ct) =>
        db.MarketingAgreements.AnyAsync(a => a.DeveloperId == developerId && a.AgreementNumber == number && (!exceptId.HasValue || a.Id != exceptId.Value), ct);
    public async Task AddAsync(MarketingAgreement a, CancellationToken ct) => await db.MarketingAgreements.AddAsync(a, ct);
    public async Task AddRevisionAsync(MarketingAgreementRevision r, CancellationToken ct)
    {
        // New app-generated child keys on a tracked root must be explicitly Added, not inferred
        // as existing Modified rows by generated-Guid tracking conventions. No SaveChanges here.
        if (loadedAgreement is not null && loadedAgreement.Id == r.MarketingAgreementId)
        {
            foreach (var rule in loadedAgreement.Rules.Where(rule => !loadedRuleIds!.Contains(rule.Id)))
                db.Entry(rule).State = EntityState.Added;
            foreach (var project in loadedAgreement.Projects.Where(project => !loadedProjectIds!.Contains(project.ProjectId)))
                db.Entry(project).State = EntityState.Added;
        }
        await db.MarketingAgreementRevisions.AddAsync(r, ct);
    }
}
