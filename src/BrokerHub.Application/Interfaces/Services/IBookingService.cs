using BrokerHub.Application.DTOs.Bookings;
namespace BrokerHub.Application.Interfaces.Services;
public interface IBookingService
{
    Task<BookingResult<BookingReply>> SubmitAsync(SubmitBookingDto request, string key, CancellationToken ct);
    Task<BookingResult<BookingPage<CustomerBookingDto>>> MineAsync(int page, int size, CancellationToken ct);
    Task<BookingResult<CustomerBookingDto>> OwnAsync(Guid id, CancellationToken ct);
    Task<BookingResult<BookingPage<CustomerBookingEventDto>>> OwnHistoryAsync(Guid id, int page, int size, CancellationToken ct);
    Task<BookingResult<CustomerBookingEventDto>> OwnEventAsync(Guid id, Guid eventId, CancellationToken ct);
    Task<BookingResult<BookingPage<StaffBookingDto>>> StaffListAsync(Guid? customerId, string? status, int page, int size, CancellationToken ct);
    Task<BookingResult<StaffBookingDto>> StaffAsync(Guid customerId, Guid id, CancellationToken ct);
    Task<BookingResult<BookingPage<StaffBookingEventDto>>> StaffHistoryAsync(Guid customerId, Guid id, int page, int size, CancellationToken ct);
    Task<BookingResult<StaffBookingEventDto>> StaffEventAsync(Guid customerId, Guid id, Guid eventId, CancellationToken ct);
}
