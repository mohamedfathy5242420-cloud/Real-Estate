namespace BrokerHub.Domain.Entities;

// Nonnullable scoped key supports reciprocal completion FK without a nullable principal key.
public sealed class DealBookingLink
{
    private DealBookingLink() { }
    public Guid DealId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid UnitId { get; private set; }
    public long BookingVersionBefore { get; private set; }
    public long BookingVersionAfter { get; private set; }
    public static DealBookingLink Record(Deal deal,Booking booking,long before)
    {
        ArgumentNullException.ThrowIfNull(deal);ArgumentNullException.ThrowIfNull(booking);
        if(before<=1||before==long.MaxValue||booking.Version!=before+1||booking.Status!=Booking.Completed||
            booking.IsActiveRequest||booking.CompletedDealId!=deal.Id||booking.CustomerId!=deal.CustomerId||
            booking.UnitId!=deal.UnitId||booking.ProjectId!=deal.ProjectId||booking.LeadId!=deal.LeadId||
            booking.CompletedAtUtc!=deal.AdoptedAtUtc||booking.CompletedByAccountId!=deal.AdoptedByAccountId)
            throw new InvalidOperationException("Exact atomic adopted Deal and completed Booking required.");
        return new(){DealId=deal.Id,BookingId=booking.Id,CustomerId=booking.CustomerId,UnitId=booking.UnitId,
            BookingVersionBefore=before,BookingVersionAfter=booking.Version};
    }
}
