using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
namespace BrokerHub.Infrastructure.Persistence.Transactions;
internal sealed class BookingExpiryStore(ApplicationDbContext db, IBookingRepository repository) : IBookingExpiryStore
{
    public async Task<IReadOnlyList<BookingExpiryCandidate>> FindDueAsync(int limit, DateTime observedUtc, CancellationToken ct)
    {
        if (limit is < 1 or > 500 || observedUtc == default || observedUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Bounded UTC expiry selection required.");
        var rows = await db.Bookings.AsNoTracking().Where(b => b.Version < long.MaxValue &&
            (b.Status == Booking.PendingReview && b.ResponseDueAtUtc <= observedUtc || b.Status == Booking.Confirmed && b.ReservationExpiresAtUtc <= observedUtc))
            .OrderBy(b => b.Status == Booking.PendingReview ? b.ResponseDueAtUtc : b.ReservationExpiresAtUtc!.Value).ThenBy(b => b.Id).Take(limit)
            .Select(b => new BookingExpiryCandidate(b.Id,b.CustomerId,b.UnitId,b.Version,b.Status,
                b.Status == Booking.PendingReview ? b.ResponseDueAtUtc : b.ReservationExpiresAtUtc!.Value)).ToArrayAsync(ct);
        return rows.Select(c => c with { EffectiveCutoffUtc = DateTime.SpecifyKind(c.EffectiveCutoffUtc,DateTimeKind.Utc) }).ToArray();
    }
    public async Task<bool> TryExpireAsync(BookingExpiryCandidate candidate, Func<Booking,BookingUnitClaim?,bool> stage, CancellationToken ct)
    {
        if (candidate.Id == Guid.Empty || candidate.CustomerId == Guid.Empty || candidate.UnitId == Guid.Empty || candidate.Version is <= 0 or long.MaxValue ||
            candidate.Status is not (Booking.PendingReview or Booking.Confirmed) || candidate.EffectiveCutoffUtc == default || candidate.EffectiveCutoffUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Actual bounded booking expiry candidate required.");
        if (db.Database.CurrentTransaction is not null || db.ChangeTracker.HasChanges()) throw new InvalidOperationException("System expiry owns a clean transaction boundary.");
        List<(EntityEntry Entry,PropertyValues Expected)> pending = [];
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(db,async (context,token) =>
            {
                context.ChangeTracker.Clear(); pending = [];
                if (await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Crm].[Customers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={candidate.CustomerId}").SingleAsync(token) != 1) return false;
                var b = await repository.LockAsync(candidate.CustomerId,candidate.Id,token);
                if (b is null || b.UnitId != candidate.UnitId || b.Version != candidate.Version || b.Status != candidate.Status ||
                    (b.Status == Booking.PendingReview ? b.ResponseDueAtUtc : b.ReservationExpiresAtUtc) != candidate.EffectiveCutoffUtc) return false;
                if (!await repository.LockUnitAsync(b.UnitId,token)) return false;
                var claim = await repository.LockClaimAsync(b.UnitId,token);
                if (!stage(b,claim))
                {
                    if (context.ChangeTracker.HasChanges()) throw new InvalidOperationException("No-op expiry must stage no changes.");
                    return false;
                }
                context.ChangeTracker.DetectChanges();
                var changed = context.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
                if (changed.Any(e => e.State == EntityState.Deleted || e.Entity is not (Booking or BookingEvent or BookingUnitClaim or SystemOutboxMessage)))
                    throw new InvalidOperationException("System booking expiry stages only its root/claim/history/System intent.");
                var history = changed.Where(e => e.State == EntityState.Added).Select(e => e.Entity).OfType<BookingEvent>().Single();
                var intent = changed.Where(e => e.State == EntityState.Added).Select(e => e.Entity).OfType<SystemOutboxMessage>().Single();
                if (b.Version != candidate.Version+1 || (candidate.Status == Booking.PendingReview ? b.Status != Booking.Rejected || b.RejectionReason != Booking.ResponseTimeout || b.RejectionActorKind != "System" || b.RejectedByAccountId is not null : b.Status != Booking.Expired) ||
                    history.BookingId != b.Id || history.CustomerId != b.CustomerId || history.BookingVersion != b.Version || history.ActorKind != "System" || history.ActorAccountId is not null || history.EffectiveCutoffUtc != candidate.EffectiveCutoffUtc ||
                    intent.Id != history.Id || intent.AggregateId != b.CustomerId || intent.ResourceId != b.Id || intent.ResourceVersion != b.Version)
                    throw new InvalidOperationException("Consistent root/System history/outbox expiry required.");
                pending = changed.Select(e => (Entry:e,Expected:e.CurrentValues.Clone())).ToList();
                await context.SaveChangesAsync(acceptAllChangesOnSuccess:false,token); return true;
            },async (context,token) =>
            {
                foreach (var (entry,expected) in pending)
                {
                    var actual = await entry.GetDatabaseValuesAsync(token);
                    if (actual is null || expected.Properties.Any(p => !Equals(expected[p],actual[p]))) return false;
                }
                return true;
            },ct);
        }
        catch (DbUpdateConcurrencyException) { return false; }
        finally { db.ChangeTracker.Clear(); }
    }
}
