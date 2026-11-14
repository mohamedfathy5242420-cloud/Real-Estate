using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace BrokerHub.Infrastructure.Persistence.Repositories;
internal sealed class BookingRepository(ApplicationDbContext db) : IBookingRepository
{
    public Task<Booking?> LockActiveAsync(Guid customerId, Guid unitId, CancellationToken ct)
    {
        RequireTransaction();
        return db.Bookings.FromSqlInterpolated($"SELECT * FROM [Crm].[Bookings] WITH (UPDLOCK,HOLDLOCK,INDEX([UX_Bookings_ActiveCustomerUnit])) WHERE [CustomerId]={customerId} AND [UnitId]={unitId} AND [IsActiveRequest]=1")
            .SingleOrDefaultAsync(ct);
    }
    public void Add(Booking b) { RequireTransaction(); db.Bookings.Add(b); }
    public Task<Booking?> LockAsync(Guid customerId, Guid id, CancellationToken ct)
    {
        RequireTransaction();
        return db.Bookings.FromSqlInterpolated($"SELECT * FROM [Crm].[Bookings] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id} AND [CustomerId]={customerId}").SingleOrDefaultAsync(ct);
    }
    public async Task<bool> LockUnitAsync(Guid unitId, CancellationToken ct)
    {
        RequireTransaction();
        return await db.Units.FromSqlInterpolated($"SELECT * FROM [Catalog].[Units] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={unitId}").AnyAsync(ct);
    }
    public Task<BookingUnitClaim?> LockClaimAsync(Guid unitId, CancellationToken ct)
    {
        RequireTransaction();
        return db.BookingUnitClaims.FromSqlInterpolated($"SELECT * FROM [Crm].[BookingUnitClaims] WITH (UPDLOCK,HOLDLOCK) WHERE [UnitId]={unitId}").SingleOrDefaultAsync(ct);
    }
    public void AddClaim(BookingUnitClaim claim) { RequireTransaction(); db.BookingUnitClaims.Add(claim); }
    public Task<bool> HasAdoptedSaleAsync(Guid unitId,CancellationToken ct)
    { RequireTransaction();return db.Deals.AsNoTracking().AnyAsync(x=>x.UnitId==unitId,ct); }
    public Task<bool> HasOtherCurrentConfirmedAsync(Guid unitId,Guid? exceptBookingId,DateTime observedUtc,CancellationToken ct)
    {
        RequireTransaction();
        // Unit is already U/HOLD protected. Shared reads (no HOLD/UPDLOCK) do not promote
        // another customer's root while its owner may be waiting for this Unit.
        return db.Bookings.FromSqlInterpolated($"SELECT * FROM [Crm].[Bookings] WITH (READCOMMITTEDLOCK,INDEX([IX_Bookings_CurrentUnitReservation])) WHERE [UnitId]={unitId} AND [Status]=N'Confirmed' AND [ReservationExpiresAtUtc]>{observedUtc}")
            .AsNoTracking().AnyAsync(x=>exceptBookingId==null||x.Id!=exceptBookingId,ct);
    }
    public void AddResponse(BookingDeveloperResponse response) { RequireTransaction(); db.BookingDeveloperResponses.Add(response); }
    public Task<BookingDeveloperResponse?> ConfirmationAsync(Guid customerId, Guid id, Guid responseId, CancellationToken ct)
    { RequireTransaction(); return db.BookingDeveloperResponses.AsNoTracking().SingleOrDefaultAsync(r => r.Id == responseId && r.BookingId == id && r.CustomerId == customerId,ct); }
    public Task<BookingExtension?> ExtensionAsync(Guid customerId, Guid id, Guid extensionId, CancellationToken ct)
    { RequireTransaction(); return db.BookingExtensions.AsNoTracking().SingleOrDefaultAsync(e => e.Id == extensionId && e.BookingId == id && e.CustomerId == customerId,ct); }
    public void AddExtension(BookingExtension extension) { RequireTransaction(); db.BookingExtensions.Add(extension); }
    public void AddDeposit(BookingDeposit deposit) { RequireTransaction(); db.BookingDeposits.Add(deposit); }
    public Task<BookingDeposit?> LockDepositAsync(Guid customerId, Guid bookingId, Guid unitId, Guid depositId, CancellationToken ct)
    {
        RequireTransaction();
        return db.BookingDeposits.FromSqlInterpolated($"SELECT * FROM [Crm].[BookingDeposits] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={depositId} AND [BookingId]={bookingId} AND [CustomerId]={customerId} AND [UnitId]={unitId}")
            .AsNoTracking().SingleOrDefaultAsync(ct);
    }
    public async Task<BookingRefundLedger> RefundLedgerAsync(Guid depositId, CancellationToken ct)
    {
        RequireTransaction();
        return await db.BookingRefunds.AsNoTracking().Where(x => x.DepositId == depositId).GroupBy(x => x.DepositId)
            .Select(g => new BookingRefundLedger(g.Sum(x => x.Amount),g.Max(x => (DateTime?)x.RecordedAtUtc))).SingleOrDefaultAsync(ct) ?? new(0,null);
    }
    public void AddRefund(BookingRefund refund) { RequireTransaction(); db.BookingRefunds.Add(refund); }
    public void AddEvent(BookingEvent h) { RequireTransaction(); db.BookingEvents.Add(h); }
    public void AddSystemEvent(SystemOutboxMessage e) { RequireTransaction(); db.SystemOutboxMessages.Add(e); }
    private void RequireTransaction()
    { if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Booking staging requires the command transaction."); }
}
