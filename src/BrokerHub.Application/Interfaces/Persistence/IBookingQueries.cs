using BrokerHub.Application.DTOs.Bookings;
namespace BrokerHub.Application.Interfaces.Persistence;
public interface IBookingQueries
{
    Task<BookingResult<BookingPage<CustomerBookingDto>>> MineAsync(Guid actor, int page, int size, CancellationToken ct);
    Task<BookingResult<CustomerBookingDto>> OwnAsync(Guid actor, Guid id, CancellationToken ct);
    Task<BookingResult<BookingPage<CustomerBookingEventDto>>> OwnHistoryAsync(Guid actor, Guid id, int page, int size, CancellationToken ct);
    Task<BookingResult<CustomerBookingEventDto>> OwnEventAsync(Guid actor, Guid id, Guid eventId, CancellationToken ct);
    Task<BookingResult<BookingPage<StaffBookingDto>>> StaffListAsync(Guid actor, Guid? customerId, string? status, int page, int size, CancellationToken ct);
    Task<BookingResult<StaffBookingDto>> StaffAsync(Guid actor, Guid customerId, Guid id, CancellationToken ct);
    Task<BookingResult<BookingPage<StaffBookingEventDto>>> StaffHistoryAsync(Guid actor, Guid customerId, Guid id, int page, int size, CancellationToken ct);
    Task<BookingResult<StaffBookingEventDto>> StaffEventAsync(Guid actor, Guid customerId, Guid id, Guid eventId, CancellationToken ct);
    Task<BookingResult<BookingPage<StaffBookingResponseDto>>> ResponsesAsync(Guid actor, Guid customerId, Guid id, int page, int size, CancellationToken ct);
    Task<BookingResult<StaffBookingResponseDto>> ResponseAsync(Guid actor, Guid customerId, Guid id, Guid responseId, CancellationToken ct);
    Task<BookingResult<BookingPage<StaffBookingExtensionDto>>> ExtensionsAsync(Guid actor, Guid customerId, Guid id, int page, int size, CancellationToken ct);
    Task<BookingResult<StaffBookingExtensionDto>> ExtensionAsync(Guid actor, Guid customerId, Guid id, Guid extensionId, CancellationToken ct);
    Task<BookingResult<BookingPage<StaffBookingDepositDto>>> DepositsAsync(Guid actor, Guid customerId, Guid id, int page, int size, CancellationToken ct);
    Task<BookingResult<StaffBookingDepositDto>> DepositAsync(Guid actor, Guid customerId, Guid id, Guid depositId, CancellationToken ct);
    Task<BookingResult<BookingPage<CustomerBookingDepositDto>>> OwnDepositsAsync(Guid actor, Guid id, int page, int size, CancellationToken ct);
    Task<BookingResult<CustomerBookingDepositDto>> OwnDepositAsync(Guid actor, Guid id, Guid depositId, CancellationToken ct);
    Task<BookingResult<BookingPage<StaffBookingRefundDto>>> RefundsAsync(Guid actor, Guid customerId, Guid id, Guid depositId, int page, int size, CancellationToken ct);
    Task<BookingResult<StaffBookingRefundDto>> RefundAsync(Guid actor, Guid customerId, Guid id, Guid depositId, Guid refundId, CancellationToken ct);
    Task<BookingResult<BookingPage<CustomerBookingRefundDto>>> OwnRefundsAsync(Guid actor, Guid id, Guid depositId, int page, int size, CancellationToken ct);
    Task<BookingResult<CustomerBookingRefundDto>> OwnRefundAsync(Guid actor, Guid id, Guid depositId, Guid refundId, CancellationToken ct);
}
