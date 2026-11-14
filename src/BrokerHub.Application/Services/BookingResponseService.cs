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
public sealed class BookingResponseService(ICrmCustomerRepository scope, IBookingRepository repository,
    ICrmFollowUpRepository authors, IBookingQueries queries, IBookingExpiryStager expiry,
    ICommandExecutor executor, ICurrentUser user, TimeProvider clock) : IBookingResponseService
{
    private Guid? Actor => user.IsAuthenticated && user.AccountId is { } actor && actor != Guid.Empty &&
        (user.IsInRole(AppRoles.SalesEmployee) || user.IsInRole(AppRoles.SalesManager)) ? actor : null;
    public Task<BookingResult<BookingPage<StaffBookingResponseDto>>> ListAsync(Guid customerId, Guid id, int page, int size, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new BookingResult<BookingPage<StaffBookingResponseDto>>(CommandStatus.Forbidden)) : page is <= 0 or > 1000000 || size is <= 0 or > 100
        ? Task.FromResult(new BookingResult<BookingPage<StaffBookingResponseDto>>(CommandStatus.Invalid)) : queries.ResponsesAsync(actor,customerId,id,page,size,ct);
    public Task<BookingResult<StaffBookingResponseDto>> GetAsync(Guid customerId, Guid id, Guid responseId, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new BookingResult<StaffBookingResponseDto>(CommandStatus.Forbidden)) : queries.ResponseAsync(actor,customerId,id,responseId,ct);
    public async Task<BookingResult<BookingReply>> RecordAsync(Guid customerId, Guid id, RecordBookingResponseDto request, string key, CancellationToken ct)
    {
        if (Actor is not { } actor) return new(CommandStatus.Forbidden);
        var currency = request.CurrencyCode?.Trim().ToUpperInvariant();
        if (customerId == Guid.Empty || id == Guid.Empty || request.Version <= 0 || request.DeveloperRespondedAtUtc == default ||
            request.DeveloperRespondedAtUtc.Offset != TimeSpan.Zero || string.IsNullOrWhiteSpace(request.Source) || request.Source.Length > BookingDeveloperResponse.SourceMaxLength ||
            string.IsNullOrWhiteSpace(request.Reference) || request.Reference.Length > BookingDeveloperResponse.ReferenceMaxLength) return new(CommandStatus.Invalid);
        if (request.ResponseKind == Booking.Confirmed)
        {
            if (request.OfferedPrice is not { } price || currency is null || request.CurrencyCode!.Length != 3 ||
                !request.CurrencyCode.All(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z') || !BookingDeveloperResponse.ValidMoney(price,currency) ||
                request.OfferedDeadlineUtc is not { } until || until == default || until.Offset != TimeSpan.Zero || request.Reason is not null) return new(CommandStatus.Invalid);
        }
        else if (request.ResponseKind != Booking.Rejected || request.OfferedPrice is not null || request.CurrencyCode is not null || request.OfferedDeadlineUtc is not null ||
            string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > BookingDeveloperResponse.ReasonMaxLength) return new(CommandStatus.Invalid);
        Booking? booking = null;
        var result = await executor.ExecuteAsync("Booking.RecordResponse",key,JsonSerializer.SerializeToElement(new {
            customerId, id, version = request.Version, responseKind = request.ResponseKind, developerRespondedAtUtc = request.DeveloperRespondedAtUtc.UtcDateTime,
            source = request.Source.Trim(), reference = request.Reference.Trim(), offeredPrice = request.OfferedPrice?.ToString("0.00",CultureInfo.InvariantCulture),
            currencyCode = currency, offeredDeadlineUtc = request.OfferedDeadlineUtc?.UtcDateTime, reason = request.Reason?.Trim() }),
            async token => {
                booking = null;
                var access = await scope.LockForSalesAsync(customerId,actor,false,token);
                if (access.Permission != CommandPermission.Allowed) return access.Permission;
                booking = await repository.LockAsync(customerId,id,token);
                return booking is null ? CommandPermission.NotFound : CommandPermission.Allowed;
            },
            async (context,token) => {
                var b = booking ?? throw new InvalidOperationException("Current Customer/Booking guard required.");
                if (b.Version != request.Version) return new CommandEffect(CommandStatus.Conflict);
                if (!await repository.LockUnitAsync(b.UnitId,token)) return new CommandEffect(CommandStatus.NotFound);
                var claim = await repository.LockClaimAsync(b.UnitId,token);
                var sold = await repository.HasAdoptedSaleAsync(b.UnitId,token);
                var employee = await authors.FindAuthorEmployeeAsync(actor,token);
                var now = clock.GetUtcNow().UtcDateTime;
                if (b.CompletedAtUtc is { } completed && now < completed) return new CommandEffect(CommandStatus.Conflict);
                if (request.DeveloperRespondedAtUtc.UtcDateTime < b.RequestedAtUtc || request.DeveloperRespondedAtUtc.UtcDateTime > now) return new CommandEffect(CommandStatus.Invalid);
                string decision;
                var changedByUser = false;
                try
                {
                    if (b.IsDue(now))
                    {
                        var pending = b.Status == Booking.PendingReview;
                        if (!expiry.Stage(b,request.Version,now,claim)) return new CommandEffect(CommandStatus.Conflict);
                        decision = pending ? BookingDeveloperResponse.ResponseTimeoutDecision : BookingDeveloperResponse.ReservationExpiredDecision;
                    }
                    else if (b.Status != Booking.PendingReview) decision = BookingDeveloperResponse.StateUnchanged;
                    else
                    {
                        if (request.ResponseKind == Booking.Rejected) { b.Reject(request.Version,"developer-rejected",now,actor); decision = BookingDeveloperResponse.DeveloperRejected; }
                        else if (request.OfferedDeadlineUtc!.Value.UtcDateTime <= now) { b.Reject(request.Version,"invalid-reservation-deadline",now,actor); decision = BookingDeveloperResponse.InvalidDeadline; }
                        else if (claim?.IsCurrent(now) == true || sold) { b.Reject(request.Version,"local-conflict",now,actor); decision = BookingDeveloperResponse.LocalConflict; }
                        else
                        {
                            var deadline = request.OfferedDeadlineUtc.Value.UtcDateTime;
                            b.Confirm(request.Version,context.CommandId,request.OfferedPrice!.Value,currency!,deadline,now,actor);
                            if (claim is null) repository.AddClaim(BookingUnitClaim.Create(b.UnitId,b.Id,deadline,now));
                            else claim.Assign(b.Id,deadline,now);
                            decision = BookingDeveloperResponse.ConfirmedDecision;
                        }
                        changedByUser = true;
                    }
                    var response = BookingDeveloperResponse.Record(context.CommandId,b,request.Version,request.ResponseKind,decision,
                        request.DeveloperRespondedAtUtc.UtcDateTime,now,actor,employee,request.Source,request.Reference,
                        request.OfferedPrice,currency,request.OfferedDeadlineUtc?.UtcDateTime,request.Reason);
                    repository.AddResponse(response);
                    if (changedByUser)
                    {
                        repository.AddEvent(BookingEvent.Decision(context.CommandId,b));
                        context.Emit(b.Status == Booking.Confirmed ? "Booking.Confirmed" : "Booking.Rejected",1,b.CustomerId,JsonSerializer.SerializeToElement(new {
                            bookingId=b.Id, customerId=b.CustomerId, unitId=b.UnitId, status=b.Status, version=b.Version, recordedAtUtc=now }));
                    }
                    context.Emit("Booking.DeveloperResponseRecorded",1,b.CustomerId,JsonSerializer.SerializeToElement(new {
                        bookingId=b.Id, customerId=b.CustomerId, unitId=b.UnitId, responseId=response.Id, decision, status=b.Status, bookingVersion=b.Version, version=response.Version }));
                    return new CommandEffect(CommandStatus.Success,new CommandOutcome("BookingDeveloperResponse",response.Id,response.Version));
                }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                catch (InvalidOperationException) { return new CommandEffect(CommandStatus.Conflict); }
            },ct);
        return result.Status == CommandStatus.Success ? new(CommandStatus.Success,new(result.Outcome!.ResourceId,result.Outcome.ResourceVersion,result.IsReplay)) : new(result.Status);
    }
}
