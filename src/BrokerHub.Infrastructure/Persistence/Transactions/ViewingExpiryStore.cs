using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BrokerHub.Infrastructure.Persistence.Transactions;
internal sealed class ViewingExpiryStore(ApplicationDbContext db) : IViewingExpiryStore
{
    public async Task<IReadOnlyList<ViewingExpiryCandidate>> FindDueAsync(int limit, DateTime observedUtc, CancellationToken ct)
    {
        if (limit is < 1 or > 500 || observedUtc == default || observedUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Bounded UTC expiry selection required.");
        return await db.Viewings.AsNoTracking().Where(v => v.Version < long.MaxValue &&
            ((v.Status == Viewing.PendingConfirmation && (v.CurrentProposedStartUtc <= observedUtc || v.PendingCycleStartedAtUtc.AddDays(7) <= observedUtc)) ||
             (v.Status == Viewing.AwaitingCustomerAcceptance && v.CurrentProposedStartUtc <= observedUtc)))
            .OrderBy(v => v.Status == Viewing.AwaitingCustomerAcceptance || v.CurrentProposedStartUtc < v.PendingCycleStartedAtUtc.AddDays(7) ? v.CurrentProposedStartUtc : v.PendingCycleStartedAtUtc.AddDays(7))
            .ThenBy(v => v.Id).Take(limit).Select(v => new ViewingExpiryCandidate(v.Id, v.CustomerId, v.Version, v.Status,
                v.PendingCycleStartedAtUtc, v.Status == Viewing.AwaitingCustomerAcceptance || v.CurrentProposedStartUtc < v.PendingCycleStartedAtUtc.AddDays(7) ? v.CurrentProposedStartUtc : v.PendingCycleStartedAtUtc.AddDays(7))).ToArrayAsync(ct);
    }
    public async Task<bool> TryExpireAsync(ViewingExpiryCandidate candidate, Func<Viewing, ViewingExpiryEffect?> stage, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null || db.ChangeTracker.HasChanges()) throw new InvalidOperationException("System expiry owns a clean transaction boundary.");
        List<(EntityEntry Entry, PropertyValues Expected)> pending = [];
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(db, async (context, token) =>
            {
                context.ChangeTracker.Clear(); pending = [];
                if (await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Crm].[Customers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={candidate.CustomerId}").SingleAsync(token) != 1) return false;
                var v = await context.Viewings.FromSqlInterpolated($"SELECT * FROM [Crm].[Viewings] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={candidate.Id} AND [CustomerId]={candidate.CustomerId}").SingleOrDefaultAsync(token);
                if (v is null || v.Version != candidate.Version || v.Status != candidate.Status || v.PendingCycleStartedAtUtc != candidate.CycleStartedAtUtc || v.PendingExpiresAtUtc != candidate.EffectiveCutoffUtc) return false;
                var effect = stage(v);
                if (effect is null)
                {
                    if (context.ChangeTracker.HasChanges()) throw new InvalidOperationException("No-op expiry must stage no changes.");
                    return false;
                }
                if (v.Status != Viewing.Expired || v.Version != candidate.Version + 1 || effect.History.ViewingId != v.Id ||
                    effect.History.CustomerId != v.CustomerId || effect.History.ViewingVersion != v.Version || effect.History.ActorKind != "System" || effect.History.ActorAccountId is not null ||
                    effect.Event.Id != effect.History.Id || effect.Event.AggregateId != v.CustomerId || effect.Event.ResourceId != v.Id || effect.Event.ResourceVersion != v.Version)
                    throw new InvalidOperationException("Consistent root/system history/outbox effect required.");
                context.ViewingEvents.Add(effect.History); context.SystemOutboxMessages.Add(effect.Event);
                context.ChangeTracker.DetectChanges(); pending = context.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified)
                    .Select(e => (Entry: e, Expected: e.CurrentValues.Clone())).ToList();
                await context.SaveChangesAsync(acceptAllChangesOnSuccess: false, token); return true;
            }, async (context, token) =>
            {
                foreach (var (entry, expected) in pending)
                {
                    var actual = await entry.GetDatabaseValuesAsync(token);
                    if (actual is null || expected.Properties.Any(p => !Equals(expected[p], actual[p]))) return false;
                }
                return true;
            }, ct);
        }
        catch (DbUpdateConcurrencyException) { return false; }
        finally { db.ChangeTracker.Clear(); }
    }
}
