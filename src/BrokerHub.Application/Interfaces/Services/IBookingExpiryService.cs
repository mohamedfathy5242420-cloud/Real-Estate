using BrokerHub.Application.DTOs.Bookings;
namespace BrokerHub.Application.Interfaces.Services;
public interface IBookingExpiryService
{
    Task<BookingExpiryBatch> ExpireDueAsync(int limit, CancellationToken ct);
    Task<bool> TryExpireAsync(BookingExpiryCandidate candidate, CancellationToken ct);
}
