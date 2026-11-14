using BrokerHub.Application.DTOs.Bookings;
namespace BrokerHub.Application.Interfaces.Services;
public interface IBookingDepositService
{
    Task<BookingResult<BookingReply>> RecordAsync(Guid customerId, Guid id, RecordBookingDepositDto request, string key, CancellationToken ct);
    Task<BookingResult<BookingPage<StaffBookingDepositDto>>> ListAsync(Guid customerId, Guid id, int page, int size, CancellationToken ct);
    Task<BookingResult<StaffBookingDepositDto>> GetAsync(Guid customerId, Guid id, Guid depositId, CancellationToken ct);
    Task<BookingResult<BookingPage<CustomerBookingDepositDto>>> OwnListAsync(Guid id, int page, int size, CancellationToken ct);
    Task<BookingResult<CustomerBookingDepositDto>> OwnAsync(Guid id, Guid depositId, CancellationToken ct);
}
