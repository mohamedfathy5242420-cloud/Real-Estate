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
public sealed class BookingDepositService(ICrmCustomerRepository scope, IBookingRepository repository,
    ICrmFollowUpRepository authors, IBookingQueries queries, ICommandExecutor executor, ICurrentUser user, TimeProvider clock) : IBookingDepositService
{
    private Guid? Actor(bool staff) => user.IsAuthenticated && user.AccountId is { } id && id != Guid.Empty &&
        (staff ? user.IsInRole(AppRoles.SalesEmployee) || user.IsInRole(AppRoles.SalesManager) : user.IsInRole(AppRoles.Customer)) ? id : null;
    private static bool ValidPage(int page, int size) => page is > 0 and <= 1000000 && size is > 0 and <= 100;
    public Task<BookingResult<BookingPage<StaffBookingDepositDto>>> ListAsync(Guid customerId, Guid id, int page, int size, CancellationToken ct) => Actor(true) is not { } actor
        ? Task.FromResult(new BookingResult<BookingPage<StaffBookingDepositDto>>(CommandStatus.Forbidden)) : !ValidPage(page,size)
        ? Task.FromResult(new BookingResult<BookingPage<StaffBookingDepositDto>>(CommandStatus.Invalid)) : queries.DepositsAsync(actor,customerId,id,page,size,ct);
    public Task<BookingResult<StaffBookingDepositDto>> GetAsync(Guid customerId, Guid id, Guid depositId, CancellationToken ct) => Actor(true) is not { } actor
        ? Task.FromResult(new BookingResult<StaffBookingDepositDto>(CommandStatus.Forbidden)) : queries.DepositAsync(actor,customerId,id,depositId,ct);
    public Task<BookingResult<BookingPage<CustomerBookingDepositDto>>> OwnListAsync(Guid id, int page, int size, CancellationToken ct) => Actor(false) is not { } actor
        ? Task.FromResult(new BookingResult<BookingPage<CustomerBookingDepositDto>>(CommandStatus.Forbidden)) : !ValidPage(page,size)
        ? Task.FromResult(new BookingResult<BookingPage<CustomerBookingDepositDto>>(CommandStatus.Invalid)) : queries.OwnDepositsAsync(actor,id,page,size,ct);
    public Task<BookingResult<CustomerBookingDepositDto>> OwnAsync(Guid id, Guid depositId, CancellationToken ct) => Actor(false) is not { } actor
        ? Task.FromResult(new BookingResult<CustomerBookingDepositDto>(CommandStatus.Forbidden)) : queries.OwnDepositAsync(actor,id,depositId,ct);
    public async Task<BookingResult<BookingReply>> RecordAsync(Guid customerId, Guid id, RecordBookingDepositDto request, string key, CancellationToken ct)
    {
        if (Actor(true) is not { } actor) return new(CommandStatus.Forbidden);
        var rawCurrency = request.CurrencyCode?.Trim();
        if (customerId == Guid.Empty || id == Guid.Empty || request.Version <= 0 || request.PaidAtUtc == default || request.PaidAtUtc.Offset != TimeSpan.Zero ||
            request.CurrencyCode?.Length > 16 || string.IsNullOrEmpty(rawCurrency) || rawCurrency.Length != 3 || rawCurrency.Any(c => c is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z')) ||
            string.IsNullOrWhiteSpace(request.Source) || request.Source.Length > BookingDeposit.SourceMaxLength ||
            string.IsNullOrWhiteSpace(request.Reference) || request.Reference.Length > BookingDeposit.ReferenceMaxLength ||
            request.Notes?.Length > BookingDeposit.NotesMaxLength) return new(CommandStatus.Invalid);
        var currency = rawCurrency.ToUpperInvariant();
        if (!BookingDeposit.ValidMoney(request.Amount,currency)) return new(CommandStatus.Invalid);
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(); Booking? booking = null;
        var result = await executor.ExecuteAsync("Booking.RecordDeposit",key,JsonSerializer.SerializeToElement(new {
            customerId,id,version=request.Version,amount=request.Amount.ToString("0.00",CultureInfo.InvariantCulture),currencyCode=currency,
            paidAtUtc=request.PaidAtUtc.UtcDateTime,source=request.Source.Trim(),reference=request.Reference.Trim(),notes }),
            async token => {
                booking = null; var access = await scope.LockForSalesAsync(customerId,actor,false,token);
                if (access.Permission != CommandPermission.Allowed) return access.Permission;
                booking = await repository.LockAsync(customerId,id,token);
                return booking is null ? CommandPermission.NotFound : CommandPermission.Allowed;
            },
            async (context,token) => {
                var b = booking ?? throw new InvalidOperationException("Current Customer/Booking guard required.");
                if (b.Version != request.Version) return new CommandEffect(CommandStatus.Conflict);
                var employee = await authors.FindAuthorEmployeeAsync(actor,token); var now = clock.GetUtcNow().UtcDateTime;
                if (!BookingDeposit.CanRecordAt(b,now)) return new CommandEffect(CommandStatus.Conflict);
                if (request.PaidAtUtc.UtcDateTime > now) return new CommandEffect(CommandStatus.Invalid);
                try {
                    var deposit = BookingDeposit.Record(context.CommandId,b,request.Version,request.Amount,currency,
                        request.PaidAtUtc.UtcDateTime,now,actor,employee,request.Source,request.Reference,notes);
                    repository.AddDeposit(deposit);
                    context.Emit("Booking.DepositRecorded",1,b.CustomerId,JsonSerializer.SerializeToElement(new {
                        depositId=deposit.Id,bookingId=b.Id,customerId=b.CustomerId,unitId=b.UnitId,bookingVersion=b.Version,depositVersion=deposit.Version }));
                    return new CommandEffect(CommandStatus.Success,new CommandOutcome("BookingDeposit",deposit.Id,deposit.Version));
                }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                catch (InvalidOperationException) { return new CommandEffect(CommandStatus.Conflict); }
            },ct);
        return result.Status == CommandStatus.Success ? new(CommandStatus.Success,new(result.Outcome!.ResourceId,result.Outcome.ResourceVersion,result.IsReplay)) : new(result.Status);
    }
}
