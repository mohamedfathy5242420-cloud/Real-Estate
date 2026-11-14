using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Application.Services;
public sealed class BookingCancellationService(ICustomerRequestScope scope, IBookingRepository repository,
    ICommandExecutor executor, ICurrentUser user, TimeProvider clock) : IBookingCancellationService
{
    public async Task<BookingResult<BookingReply>> CancelAsync(Guid id, CancelBookingDto request, string key, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.AccountId is not { } actor || actor == Guid.Empty || !user.IsInRole(AppRoles.Customer)) return new(CommandStatus.Forbidden);
        if (id == Guid.Empty || request.Version <= 0 || request.Reason?.Length > Booking.CancellationReasonMaxLength) return new(CommandStatus.Invalid);
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(); Booking? booking = null;
        var result = await executor.ExecuteAsync("Booking.Cancel",key,JsonSerializer.SerializeToElement(new {id,version=request.Version,reason}),
            async token => {
                booking = null;var access = await scope.LockOwnedAsync(actor,token);
                if (access.Permission != CommandPermission.Allowed) return access.Permission;
                booking = await repository.LockAsync(access.Customer!.Id,id,token);
                return booking is null ? CommandPermission.NotFound : CommandPermission.Allowed;
            },
            async (context,token) => {
                var b = booking ?? throw new InvalidOperationException("Current owned Customer/Booking guard required.");
                if (b.Status is not (Booking.PendingReview or Booking.Confirmed) || b.Version != request.Version || b.Version == long.MaxValue) return new CommandEffect(CommandStatus.Conflict);
                if (!await repository.LockUnitAsync(b.UnitId,token)) return new CommandEffect(CommandStatus.NotFound);
                var claim = await repository.LockClaimAsync(b.UnitId,token);var now = clock.GetUtcNow().UtcDateTime;
                if (b.IsDue(now) || now < b.RequestedAtUtc || now < b.ConfirmedAtUtc || now < b.LastExtendedAtUtc) return new CommandEffect(CommandStatus.Conflict);
                try
                {
                    var previous = b.Status;b.Cancel(request.Version,now,actor,reason);
                    if (previous == Booking.Confirmed) claim?.Release(b.Id,now);
                    repository.AddEvent(BookingEvent.Cancellation(context.CommandId,b));
                    context.Emit("Booking.Cancelled",1,b.CustomerId,JsonSerializer.SerializeToElement(new {
                        bookingId=b.Id,customerId=b.CustomerId,unitId=b.UnitId,previousStatus=previous,status=b.Status,version=b.Version,cancelledAtUtc=now }));
                    return new CommandEffect(CommandStatus.Success,new CommandOutcome("Booking",b.Id,b.Version));
                }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                catch (InvalidOperationException) { return new CommandEffect(CommandStatus.Conflict); }
            },ct);
        return result.Status == CommandStatus.Success ? new(CommandStatus.Success,new(result.Outcome!.ResourceId,result.Outcome.ResourceVersion,result.IsReplay)) : new(result.Status);
    }
}
