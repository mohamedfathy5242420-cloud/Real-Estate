using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Application.Services;
public sealed class BookingExtensionService(ICrmCustomerRepository scope, IBookingRepository repository,
    ICrmFollowUpRepository authors, IBookingQueries queries, ICommandExecutor executor, ICurrentUser user, TimeProvider clock) : IBookingExtensionService
{
    private Guid? Actor => user.IsAuthenticated && user.AccountId is { } actor && actor != Guid.Empty &&
        (user.IsInRole(AppRoles.SalesEmployee) || user.IsInRole(AppRoles.SalesManager)) ? actor : null;
    public Task<BookingResult<BookingPage<StaffBookingExtensionDto>>> ListAsync(Guid customerId, Guid id, int page, int size, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new BookingResult<BookingPage<StaffBookingExtensionDto>>(CommandStatus.Forbidden)) : page is <= 0 or > 1000000 || size is <= 0 or > 100
        ? Task.FromResult(new BookingResult<BookingPage<StaffBookingExtensionDto>>(CommandStatus.Invalid)) : queries.ExtensionsAsync(actor,customerId,id,page,size,ct);
    public Task<BookingResult<StaffBookingExtensionDto>> GetAsync(Guid customerId, Guid id, Guid extensionId, CancellationToken ct) => Actor is not { } actor
        ? Task.FromResult(new BookingResult<StaffBookingExtensionDto>(CommandStatus.Forbidden)) : queries.ExtensionAsync(actor,customerId,id,extensionId,ct);
    public async Task<BookingResult<BookingReply>> RecordAsync(Guid customerId, Guid id, RecordBookingExtensionDto request, string key, CancellationToken ct)
    {
        if (Actor is not { } actor) return new(CommandStatus.Forbidden);
        if (customerId == Guid.Empty || id == Guid.Empty || request.Version <= 0 || request.DeveloperExtendedAtUtc == default || request.DeveloperExtendedAtUtc.Offset != TimeSpan.Zero ||
            request.NewReservationExpiresAtUtc == default || request.NewReservationExpiresAtUtc.Offset != TimeSpan.Zero ||
            string.IsNullOrWhiteSpace(request.Source) || request.Source.Length > BookingExtension.SourceMaxLength ||
            string.IsNullOrWhiteSpace(request.Reference) || request.Reference.Length > BookingExtension.ReferenceMaxLength || request.Reason?.Length > BookingExtension.ReasonMaxLength)
            return new(CommandStatus.Invalid);
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        Booking? booking = null;
        var result = await executor.ExecuteAsync("Booking.Extend",key,JsonSerializer.SerializeToElement(new {
            customerId,id,version=request.Version,developerExtendedAtUtc=request.DeveloperExtendedAtUtc.UtcDateTime,
            newReservationExpiresAtUtc=request.NewReservationExpiresAtUtc.UtcDateTime,source=request.Source.Trim(),reference=request.Reference.Trim(),reason }),
            async token => {
                booking = null;
                var access = await scope.LockForSalesAsync(customerId,actor,false,token);
                if (access.Permission != CommandPermission.Allowed) return access.Permission;
                booking = await repository.LockAsync(customerId,id,token);
                return booking is null ? CommandPermission.NotFound : CommandPermission.Allowed;
            },
            async (context,token) => {
                var b = booking ?? throw new InvalidOperationException("Current Customer/Booking guard required.");
                if (b.Status != Booking.Confirmed || b.Version != request.Version || b.Version == long.MaxValue || b.ReservationExpiresAtUtc is not { } old || b.ConfirmedResponseId is not { } originalId)
                    return new CommandEffect(CommandStatus.Conflict);
                var original = await repository.ConfirmationAsync(b.CustomerId,b.Id,originalId,token);
                var last = b.LastExtensionId is { } lastId ? await repository.ExtensionAsync(b.CustomerId,b.Id,lastId,token) : null;
                if (original is null || original.Decision != BookingDeveloperResponse.ConfirmedDecision ||
                    b.LastExtensionId.HasValue && (last is null || last.NewReservationExpiresAtUtc != old || last.RecordedAtUtc != b.LastExtendedAtUtc))
                    return new CommandEffect(CommandStatus.Conflict);
                if (!await repository.LockUnitAsync(b.UnitId,token)) return new CommandEffect(CommandStatus.NotFound);
                var claim = await repository.LockClaimAsync(b.UnitId,token);
                var employee = await authors.FindAuthorEmployeeAsync(actor,token);
                var now = clock.GetUtcNow().UtcDateTime; var deadline = request.NewReservationExpiresAtUtc.UtcDateTime;
                if (now >= old || now < b.ConfirmedAtUtc || now < b.LastExtendedAtUtc || deadline <= old || claim is null || claim.Version == long.MaxValue ||
                    claim.BookingId != b.Id || claim.ExpiresAtUtc != old || !claim.IsCurrent(now) || now < claim.UpdatedAtUtc)
                    return new CommandEffect(CommandStatus.Conflict);
                var occurred = request.DeveloperExtendedAtUtc.UtcDateTime;
                if (occurred < original.DeveloperRespondedAtUtc || occurred < last?.DeveloperExtendedAtUtc || occurred > now) return new CommandEffect(CommandStatus.Invalid);
                try
                {
                    var previous = DateTime.SpecifyKind(old,DateTimeKind.Utc);
                    b.Extend(request.Version,context.CommandId,deadline,now); claim.Extend(b.Id,previous,deadline,now);
                    var extension = BookingExtension.Record(context.CommandId,b,request.Version,previous,occurred,now,actor,employee,request.Source,request.Reference,reason);
                    repository.AddExtension(extension); repository.AddEvent(BookingEvent.Extended(extension,b));
                    context.Emit("Booking.Extended",1,b.CustomerId,JsonSerializer.SerializeToElement(new {
                        bookingId=b.Id,customerId=b.CustomerId,unitId=b.UnitId,extensionId=extension.Id,
                        previousReservationExpiresAtUtc=previous,reservationExpiresAtUtc=deadline,bookingVersion=b.Version,version=extension.Version }));
                    return new CommandEffect(CommandStatus.Success,new CommandOutcome("BookingExtension",extension.Id,extension.Version));
                }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                catch (InvalidOperationException) { return new CommandEffect(CommandStatus.Conflict); }
            },ct);
        return result.Status == CommandStatus.Success ? new(CommandStatus.Success,new(result.Outcome!.ResourceId,result.Outcome.ResourceVersion,result.IsReplay)) : new(result.Status);
    }
}
