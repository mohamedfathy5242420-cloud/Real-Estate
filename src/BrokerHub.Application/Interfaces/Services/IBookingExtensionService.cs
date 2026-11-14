using BrokerHub.Application.DTOs.Bookings;
namespace BrokerHub.Application.Interfaces.Services;
public interface IBookingExtensionService
{
    Task<BookingResult<BookingReply>> RecordAsync(Guid customerId, Guid id, RecordBookingExtensionDto request, string key, CancellationToken ct);
    Task<BookingResult<BookingPage<StaffBookingExtensionDto>>> ListAsync(Guid customerId, Guid id, int page, int size, CancellationToken ct);
    Task<BookingResult<StaffBookingExtensionDto>> GetAsync(Guid customerId, Guid id, Guid extensionId, CancellationToken ct);
}
