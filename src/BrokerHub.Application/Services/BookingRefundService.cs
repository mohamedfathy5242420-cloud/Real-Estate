using System.Globalization;
using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Application.Services;
public sealed class BookingRefundService(ICrmCustomerRepository scope, IBookingRepository repository,
    ICrmFollowUpRepository authors, IBookingQueries queries, ICommandExecutor executor, ICurrentUser user, TimeProvider clock) : IBookingRefundService
{
    private Guid? Actor(bool staff) => user.IsAuthenticated && user.AccountId is { } id && id != Guid.Empty &&
        (staff ? user.IsInRole(AppRoles.SalesEmployee) || user.IsInRole(AppRoles.SalesManager) : user.IsInRole(AppRoles.Customer)) ? id : null;
    private static bool Page(int page, int size) => page is > 0 and <= 1000000 && size is > 0 and <= 100;
    public Task<BookingResult<BookingPage<StaffBookingRefundDto>>> ListAsync(Guid customerId, Guid id, Guid depositId, int page, int size, CancellationToken ct) => Actor(true) is not { } actor
        ? Task.FromResult(new BookingResult<BookingPage<StaffBookingRefundDto>>(CommandStatus.Forbidden)) : !Page(page,size)
        ? Task.FromResult(new BookingResult<BookingPage<StaffBookingRefundDto>>(CommandStatus.Invalid)) : queries.RefundsAsync(actor,customerId,id,depositId,page,size,ct);
    public Task<BookingResult<StaffBookingRefundDto>> GetAsync(Guid customerId, Guid id, Guid depositId, Guid refundId, CancellationToken ct) => Actor(true) is not { } actor
        ? Task.FromResult(new BookingResult<StaffBookingRefundDto>(CommandStatus.Forbidden)) : queries.RefundAsync(actor,customerId,id,depositId,refundId,ct);
    public Task<BookingResult<BookingPage<CustomerBookingRefundDto>>> OwnListAsync(Guid id, Guid depositId, int page, int size, CancellationToken ct) => Actor(false) is not { } actor
        ? Task.FromResult(new BookingResult<BookingPage<CustomerBookingRefundDto>>(CommandStatus.Forbidden)) : !Page(page,size)
        ? Task.FromResult(new BookingResult<BookingPage<CustomerBookingRefundDto>>(CommandStatus.Invalid)) : queries.OwnRefundsAsync(actor,id,depositId,page,size,ct);
    public Task<BookingResult<CustomerBookingRefundDto>> OwnAsync(Guid id, Guid depositId, Guid refundId, CancellationToken ct) => Actor(false) is not { } actor
        ? Task.FromResult(new BookingResult<CustomerBookingRefundDto>(CommandStatus.Forbidden)) : queries.OwnRefundAsync(actor,id,depositId,refundId,ct);
    public async Task<BookingResult<BookingReply>> RecordAsync(Guid customerId, Guid id, Guid depositId, RecordBookingRefundDto request, string key, CancellationToken ct)
    {
        if (Actor(true) is not { } actor) return new(CommandStatus.Forbidden);
        var rawCurrency=request.CurrencyCode?.Trim();
        if (customerId == Guid.Empty || id == Guid.Empty || depositId == Guid.Empty || request.Version <= 0 ||
            request.ReturnedAtUtc == default || request.ReturnedAtUtc.Offset != TimeSpan.Zero || request.CurrencyCode?.Length > 16 ||
            string.IsNullOrEmpty(rawCurrency) || rawCurrency.Length != 3 || rawCurrency.Any(c => c is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z')) ||
            string.IsNullOrWhiteSpace(request.Source) || request.Source.Length > BookingRefund.SourceMaxLength ||
            string.IsNullOrWhiteSpace(request.Reference) || request.Reference.Length > BookingRefund.ReferenceMaxLength ||
            request.Notes?.Length > BookingRefund.NotesMaxLength) return new(CommandStatus.Invalid);
        var currency=rawCurrency.ToUpperInvariant();
        if (!BookingDeposit.ValidMoney(request.Amount,currency)) return new(CommandStatus.Invalid);
        var notes=string.IsNullOrWhiteSpace(request.Notes)?null:request.Notes.Trim(); Booking? booking=null; BookingDeposit? deposit=null;
        var result=await executor.ExecuteAsync("Booking.RecordRefund",key,JsonSerializer.SerializeToElement(new {
            customerId,id,depositId,version=request.Version,amount=request.Amount.ToString("0.00",CultureInfo.InvariantCulture),currencyCode=currency,
            returnedAtUtc=request.ReturnedAtUtc.UtcDateTime,source=request.Source.Trim(),reference=request.Reference.Trim(),notes }),
            async token => {
                booking=null;deposit=null;var access=await scope.LockForSalesAsync(customerId,actor,false,token);
                if (access.Permission != CommandPermission.Allowed) return access.Permission;
                booking=await repository.LockAsync(customerId,id,token);
                if (booking is null) return CommandPermission.NotFound;
                deposit=await repository.LockDepositAsync(booking.CustomerId,booking.Id,booking.UnitId,depositId,token);
                return deposit is null?CommandPermission.NotFound:CommandPermission.Allowed;
            },
            async (context,token) => {
                var b=booking??throw new InvalidOperationException("Current Booking guard required.");
                var d=deposit??throw new InvalidOperationException("Current same-booking deposit guard required.");
                if (b.Version != request.Version) return new CommandEffect(CommandStatus.Conflict);
                var ledger=await repository.RefundLedgerAsync(d.Id,token);
                var employee=await authors.FindAuthorEmployeeAsync(actor,token);var now=clock.GetUtcNow().UtcDateTime;
                if (!BookingRefund.CanRecordAt(b,d,ledger.LastRecordedAtUtc,now) || ledger.RecordedAmount < 0 || ledger.RecordedAmount > d.Amount)
                    return new CommandEffect(CommandStatus.Conflict);
                if (currency != d.CurrencyCode || request.ReturnedAtUtc.UtcDateTime < d.PaidAtUtc || request.ReturnedAtUtc.UtcDateTime > now)
                    return new CommandEffect(CommandStatus.Invalid);
                if (request.Amount > d.Amount-ledger.RecordedAmount) return new CommandEffect(CommandStatus.Conflict);
                try {
                    var refund=BookingRefund.Record(context.CommandId,b,d,request.Version,ledger.RecordedAmount,ledger.LastRecordedAtUtc,
                        request.Amount,currency,request.ReturnedAtUtc.UtcDateTime,now,actor,employee,request.Source,request.Reference,notes);
                    repository.AddRefund(refund);
                    context.Emit("Booking.RefundRecorded",1,b.CustomerId,JsonSerializer.SerializeToElement(new {
                        refundId=refund.Id,depositId=d.Id,bookingId=b.Id,customerId=b.CustomerId,unitId=b.UnitId,bookingVersion=b.Version,refundVersion=refund.Version }));
                    return new CommandEffect(CommandStatus.Success,new CommandOutcome("BookingRefund",refund.Id,refund.Version));
                }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                catch (InvalidOperationException) { return new CommandEffect(CommandStatus.Conflict); }
            },ct);
        return result.Status == CommandStatus.Success?new(CommandStatus.Success,new(result.Outcome!.ResourceId,result.Outcome.ResourceVersion,result.IsReplay)):new(result.Status);
    }
}
