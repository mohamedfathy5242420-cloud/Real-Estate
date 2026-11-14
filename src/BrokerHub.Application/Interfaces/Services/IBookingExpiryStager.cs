using BrokerHub.Domain.Entities;
namespace BrokerHub.Application.Interfaces.Services;
public interface IBookingExpiryStager
{
    // Caller owns the root and Unit/claim locks in its command transaction. Stages only.
    bool Stage(Booking booking, long version, DateTime now, BookingUnitClaim? claim);
}
