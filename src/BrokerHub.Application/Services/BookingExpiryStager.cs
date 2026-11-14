using System.Text.Json;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Serialization;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Application.Services;
public sealed class BookingExpiryStager(IBookingRepository repository) : IBookingExpiryStager
{
    public bool Stage(Booking booking, long version, DateTime now, BookingUnitClaim? claim)
    {
        if (!booking.TryEndIfDue(version,now)) return false;
        var reservation = booking.Status == Booking.Expired;
        var history = reservation ? BookingEvent.Expiry(Guid.NewGuid(),booking) : BookingEvent.Timeout(Guid.NewGuid(),booking);
        if (reservation) claim?.Release(booking.Id,now);
        repository.AddEvent(history);
        repository.AddSystemEvent(SystemOutboxMessage.Record(history.Id,reservation ? "System.BookingExpiry" : "System.BookingResponseTimeout",
            reservation ? "Booking.Expired" : "Booking.ResponseTimedOut",1,booking.CustomerId,booking.Id,booking.Version,
            CanonicalCommandJson.Canonicalize(JsonSerializer.SerializeToElement(new {
                bookingId = booking.Id, customerId = booking.CustomerId, unitId = booking.UnitId,
                responseDueAtUtc = DateTime.SpecifyKind(reservation ? booking.ReservationExpiresAtUtc!.Value : booking.ResponseDueAtUtc,DateTimeKind.Utc),
                detectedAtUtc = now, reasonCode = reservation ? "reservation-expired" : Booking.ResponseTimeout, version = booking.Version })),now,now));
        return true;
    }
}
