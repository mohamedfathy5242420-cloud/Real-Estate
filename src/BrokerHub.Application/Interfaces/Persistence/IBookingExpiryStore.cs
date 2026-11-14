using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Application.Interfaces.Persistence;
public interface IBookingExpiryStore
{
    Task<IReadOnlyList<BookingExpiryCandidate>> FindDueAsync(int limit, DateTime observedUtc, CancellationToken ct);
    Task<bool> TryExpireAsync(BookingExpiryCandidate candidate, Func<Booking,BookingUnitClaim?,bool> stage, CancellationToken ct);
}
