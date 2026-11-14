using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Serialization;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;
public sealed class BookingService(ICustomerRequestScope scope, IRequestUnitRepository units, ILeadRepository leads,
    ILeadOpportunityService opportunities, IBookingRepository repository, IBookingQueries queries,
    ICommandExecutor executor, ICurrentUser user, TimeProvider clock, IBookingExpiryStager expiry) : IBookingService
{
    private Guid? Actor(bool staff) => user.IsAuthenticated && user.AccountId is { } actor && actor != Guid.Empty &&
        (staff ? user.IsInRole(AppRoles.SalesEmployee) || user.IsInRole(AppRoles.SalesManager) : user.IsInRole(AppRoles.Customer)) ? actor : null;
    private static bool Page(int page, int size) => page is > 0 and <= 1000000 && size is > 0 and <= 100;
    public Task<BookingResult<BookingPage<CustomerBookingDto>>> MineAsync(int page, int size, CancellationToken ct) => Actor(false) is not { } actor
        ? Task.FromResult(new BookingResult<BookingPage<CustomerBookingDto>>(CommandStatus.Forbidden)) : !Page(page,size)
        ? Task.FromResult(new BookingResult<BookingPage<CustomerBookingDto>>(CommandStatus.Invalid)) : queries.MineAsync(actor,page,size,ct);
    public Task<BookingResult<CustomerBookingDto>> OwnAsync(Guid id, CancellationToken ct) => Actor(false) is not { } actor
        ? Task.FromResult(new BookingResult<CustomerBookingDto>(CommandStatus.Forbidden)) : queries.OwnAsync(actor,id,ct);
    public Task<BookingResult<BookingPage<CustomerBookingEventDto>>> OwnHistoryAsync(Guid id, int page, int size, CancellationToken ct) => Actor(false) is not { } actor
        ? Task.FromResult(new BookingResult<BookingPage<CustomerBookingEventDto>>(CommandStatus.Forbidden)) : !Page(page,size)
        ? Task.FromResult(new BookingResult<BookingPage<CustomerBookingEventDto>>(CommandStatus.Invalid)) : queries.OwnHistoryAsync(actor,id,page,size,ct);
    public Task<BookingResult<CustomerBookingEventDto>> OwnEventAsync(Guid id, Guid eventId, CancellationToken ct) => Actor(false) is not { } actor
        ? Task.FromResult(new BookingResult<CustomerBookingEventDto>(CommandStatus.Forbidden)) : queries.OwnEventAsync(actor,id,eventId,ct);
    public Task<BookingResult<BookingPage<StaffBookingDto>>> StaffListAsync(Guid? customerId, string? status, int page, int size, CancellationToken ct) => Actor(true) is not { } actor
        ? Task.FromResult(new BookingResult<BookingPage<StaffBookingDto>>(CommandStatus.Forbidden)) : !Page(page,size) || status is not (null or Booking.PendingReview or Booking.Rejected or Booking.Confirmed or Booking.Expired or Booking.Cancelled or Booking.Completed)
        ? Task.FromResult(new BookingResult<BookingPage<StaffBookingDto>>(CommandStatus.Invalid)) : queries.StaffListAsync(actor,customerId,status,page,size,ct);
    public Task<BookingResult<StaffBookingDto>> StaffAsync(Guid customerId, Guid id, CancellationToken ct) => Actor(true) is not { } actor
        ? Task.FromResult(new BookingResult<StaffBookingDto>(CommandStatus.Forbidden)) : queries.StaffAsync(actor,customerId,id,ct);
    public Task<BookingResult<BookingPage<StaffBookingEventDto>>> StaffHistoryAsync(Guid customerId, Guid id, int page, int size, CancellationToken ct) => Actor(true) is not { } actor
        ? Task.FromResult(new BookingResult<BookingPage<StaffBookingEventDto>>(CommandStatus.Forbidden)) : !Page(page,size)
        ? Task.FromResult(new BookingResult<BookingPage<StaffBookingEventDto>>(CommandStatus.Invalid)) : queries.StaffHistoryAsync(actor,customerId,id,page,size,ct);
    public Task<BookingResult<StaffBookingEventDto>> StaffEventAsync(Guid customerId, Guid id, Guid eventId, CancellationToken ct) => Actor(true) is not { } actor
        ? Task.FromResult(new BookingResult<StaffBookingEventDto>(CommandStatus.Forbidden)) : queries.StaffEventAsync(actor,customerId,id,eventId,ct);

    public async Task<BookingResult<BookingReply>> SubmitAsync(SubmitBookingDto request, string key, CancellationToken ct)
    {
        if (Actor(false) is not { } actor) return new(CommandStatus.Forbidden);
        if (request.UnitId == Guid.Empty || request.RequestReason?.Trim().Length > Booking.RequestReasonMaxLength) return new(CommandStatus.Invalid);
        var reason = string.IsNullOrWhiteSpace(request.RequestReason) ? null : request.RequestReason.Trim();
        Customer? customer = null;
        var result = await executor.ExecuteAsync("Booking.Submit", key, JsonSerializer.SerializeToElement(new { unitId = request.UnitId, requestReason = reason }),
            async token => { customer = null; var access = await scope.LockOwnedAsync(actor,token); customer = access.Customer; return access.Permission; },
            async (context,token) =>
            {
                var owner = customer ?? throw new InvalidOperationException("Current owned Customer lock required.");
                var old = await repository.LockActiveAsync(owner.Id,request.UnitId,token);
                if (old is not null && (!old.IsDue(clock.GetUtcNow().UtcDateTime) || old.Version == long.MaxValue)) return new CommandEffect(CommandStatus.Conflict);
                var identity = await units.ResolveIdentityAsync(request.UnitId,token);
                if (identity is null) return new CommandEffect(CommandStatus.NotFound);
                await leads.LockOpenAsync(owner.Id,identity.ProjectId,token);
                var unit = await units.LockAsync(identity,token);
                if (unit is null || !unit.Published || !unit.DeveloperActive || !unit.ProjectActive) return new CommandEffect(CommandStatus.NotFound);
                TimeZoneInfo zone;
                try { zone = TimeZoneInfo.FindSystemTimeZoneById(unit.TimeZoneId); }
                catch (TimeZoneNotFoundException) { return new CommandEffect(CommandStatus.Conflict); }
                catch (InvalidTimeZoneException) { return new CommandEffect(CommandStatus.Conflict); }
                var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(),zone).DateTime);
                var origins = await leads.LockAgreementsAsync(unit.DeveloperId,unit.ProjectId,localDate,token);
                if (origins.Count == 0) return new CommandEffect(CommandStatus.NotFound);
                if (origins.Count != 1) return new CommandEffect(CommandStatus.Conflict);
                var original = await opportunities.EnsureOpenAsync(owner.Id,unit.ProjectId,"BookingRequest",context,token);
                if (original.Status != CommandStatus.Success) return new CommandEffect(original.Status);
                var now = clock.GetUtcNow(); var finalDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now,zone).DateTime);
                if (now.UtcDateTime < owner.CreatedAtUtc) return new CommandEffect(CommandStatus.Conflict);
                if (finalDate != localDate)
                {
                    origins = await leads.LockAgreementsAsync(unit.DeveloperId,unit.ProjectId,finalDate,token);
                    if (origins.Count == 0) return new CommandEffect(CommandStatus.NotFound);
                    if (origins.Count != 1) return new CommandEffect(CommandStatus.Conflict);
                }
                if (old is not null)
                {
                    var claim = await repository.LockClaimAsync(old.UnitId,token);
                    now = clock.GetUtcNow();
                    // A late claim wait can cross the project day; retain fresh public qualification.
                    var claimDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now,zone).DateTime);
                    if (claimDate != finalDate)
                    {
                        origins = await leads.LockAgreementsAsync(unit.DeveloperId,unit.ProjectId,claimDate,token);
                        if (origins.Count == 0) return new CommandEffect(CommandStatus.NotFound);
                        if (origins.Count != 1) return new CommandEffect(CommandStatus.Conflict);
                        finalDate = claimDate;
                    }
                    if (!expiry.Stage(old,old.Version,now.UtcDateTime,claim)) return new CommandEffect(CommandStatus.Conflict);
                }
                Booking booking;
                try { booking = Booking.Request(context.CommandId,owner.Id,original.Lead!.Id,unit.UnitId,unit.ProjectId,origins[0].Id,
                    finalDate,unit.TimeZoneId,reason,now.UtcDateTime,context.ActorAccountId); }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                repository.Add(booking); repository.AddEvent(BookingEvent.Requested(context.CommandId,booking));
                context.Emit("Booking.Requested",1,owner.Id,JsonSerializer.SerializeToElement(new {
                    bookingId = booking.Id, customerId = booking.CustomerId, unitId = booking.UnitId, projectId = booking.ProjectId,
                    requestedAtUtc = booking.RequestedAtUtc, responseDueAtUtc = booking.ResponseDueAtUtc, version = booking.Version }));
                return new CommandEffect(CommandStatus.Success,new CommandOutcome("Booking",booking.Id,booking.Version));
            },ct);
        return result.Status == CommandStatus.Success ? new(CommandStatus.Success,new(result.Outcome!.ResourceId,result.Outcome.ResourceVersion,result.IsReplay)) : new(result.Status);
    }
}
