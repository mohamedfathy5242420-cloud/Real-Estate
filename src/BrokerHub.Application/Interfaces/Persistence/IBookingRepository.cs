using BrokerHub.Domain.Entities;
using BrokerHub.Application.DTOs.Bookings;
namespace BrokerHub.Application.Interfaces.Persistence;
public interface IBookingRepository
{
    Task<Booking?> LockActiveAsync(Guid customerId, Guid unitId, CancellationToken ct);
    Task<Booking?> LockAsync(Guid customerId, Guid id, CancellationToken ct);
    Task<bool> LockUnitAsync(Guid unitId, CancellationToken ct);
    Task<BookingUnitClaim?> LockClaimAsync(Guid unitId, CancellationToken ct);
    Task<bool> HasAdoptedSaleAsync(Guid unitId,CancellationToken ct);
    Task<bool> HasOtherCurrentConfirmedAsync(Guid unitId,Guid? exceptBookingId,DateTime observedUtc,CancellationToken ct);
    void AddClaim(BookingUnitClaim claim);
    void AddResponse(BookingDeveloperResponse response);
    Task<BookingDeveloperResponse?> ConfirmationAsync(Guid customerId, Guid id, Guid responseId, CancellationToken ct);
    Task<BookingExtension?> ExtensionAsync(Guid customerId, Guid id, Guid extensionId, CancellationToken ct);
    void AddExtension(BookingExtension extension);
    void AddDeposit(BookingDeposit deposit);
    Task<BookingDeposit?> LockDepositAsync(Guid customerId, Guid bookingId, Guid unitId, Guid depositId, CancellationToken ct);
    Task<BookingRefundLedger> RefundLedgerAsync(Guid depositId, CancellationToken ct);
    void AddRefund(BookingRefund refund);
    void Add(Booking booking);
    void AddEvent(BookingEvent history);
    void AddSystemEvent(SystemOutboxMessage intent);
}
