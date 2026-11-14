using BrokerHub.Application.DTOs.Bookings;
namespace BrokerHub.Application.Interfaces.Services;
public interface IBookingCancellationService
{
    Task<BookingResult<BookingReply>> CancelAsync(Guid id, CancelBookingDto request, string key, CancellationToken ct);
}
