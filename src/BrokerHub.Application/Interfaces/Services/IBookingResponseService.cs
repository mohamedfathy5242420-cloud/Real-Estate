using BrokerHub.Application.DTOs.Bookings;
namespace BrokerHub.Application.Interfaces.Services;
public interface IBookingResponseService
{
    Task<BookingResult<BookingReply>> RecordAsync(Guid customerId, Guid id, RecordBookingResponseDto request, string key, CancellationToken ct);
    Task<BookingResult<BookingPage<StaffBookingResponseDto>>> ListAsync(Guid customerId, Guid id, int page, int size, CancellationToken ct);
    Task<BookingResult<StaffBookingResponseDto>> GetAsync(Guid customerId, Guid id, Guid responseId, CancellationToken ct);
}
