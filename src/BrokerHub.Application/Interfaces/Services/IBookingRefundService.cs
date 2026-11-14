using BrokerHub.Application.DTOs.Bookings;
namespace BrokerHub.Application.Interfaces.Services;
public interface IBookingRefundService
{
    Task<BookingResult<BookingReply>> RecordAsync(Guid customerId, Guid id, Guid depositId, RecordBookingRefundDto request, string key, CancellationToken ct);
    Task<BookingResult<BookingPage<StaffBookingRefundDto>>> ListAsync(Guid customerId, Guid id, Guid depositId, int page, int size, CancellationToken ct);
    Task<BookingResult<StaffBookingRefundDto>> GetAsync(Guid customerId, Guid id, Guid depositId, Guid refundId, CancellationToken ct);
    Task<BookingResult<BookingPage<CustomerBookingRefundDto>>> OwnListAsync(Guid id, Guid depositId, int page, int size, CancellationToken ct);
    Task<BookingResult<CustomerBookingRefundDto>> OwnAsync(Guid id, Guid depositId, Guid refundId, CancellationToken ct);
}
