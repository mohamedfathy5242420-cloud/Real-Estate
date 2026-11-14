using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
namespace BrokerHub.Application.Services;
public sealed class BookingExpiryService(IBookingExpiryStore store, IBookingExpiryStager expiry, TimeProvider clock) : IBookingExpiryService
{
    public async Task<BookingExpiryBatch> ExpireDueAsync(int limit, CancellationToken ct)
    {
        if (limit is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(limit));
        var candidates = await store.FindDueAsync(limit,clock.GetUtcNow().UtcDateTime,ct); var expired = 0;
        foreach (var candidate in candidates) { ct.ThrowIfCancellationRequested(); if (await TryExpireAsync(candidate,ct)) expired++; }
        return new(candidates.Count,expired);
    }
    public Task<bool> TryExpireAsync(BookingExpiryCandidate candidate, CancellationToken ct) =>
        store.TryExpireAsync(candidate,(booking,claim) => expiry.Stage(booking,candidate.Version,clock.GetUtcNow().UtcDateTime,claim),ct);
}
