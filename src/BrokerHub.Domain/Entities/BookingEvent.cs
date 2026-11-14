namespace BrokerHub.Domain.Entities;

public sealed class BookingEvent
{
    private BookingEvent() { }
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid CustomerId { get; private set; }
    public long BookingVersion { get; private set; }
    public string Kind { get; private set; } = "";
    public string ActorKind { get; private set; } = "User";
    public Guid? ActorAccountId { get; private set; }
    public DateTime AtUtc { get; private set; }
    public string Status { get; private set; } = "";
    public string? ReasonCode { get; private set; }
    public DateTime? EffectiveCutoffUtc { get; private set; }
    public decimal? ConfirmedPrice { get; private set; }
    public string? ConfirmedCurrencyCode { get; private set; }
    public DateTime? ReservationExpiresAtUtc { get; private set; }
    public Guid? DeveloperResponseId { get; private set; }
    public DateTime? PreviousReservationExpiresAtUtc { get; private set; }
    public Guid? ExtensionId { get; private set; }
    public string? CancelledFromStatus { get; private set; }
    public string? CancellationReason { get; private set; }
    public Guid? DealId { get; private set; }
    public static BookingEvent Completion(Guid id,Booking b,Deal deal)
    {
        if(id==Guid.Empty||b.Status!=Booking.Completed||b.IsActiveRequest||b.Version<=2||b.CompletedDealId!=deal.Id||
            b.CustomerId!=deal.CustomerId||b.UnitId!=deal.UnitId||b.CompletedAtUtc!=deal.AdoptedAtUtc||
            b.CompletedByAccountId!=deal.AdoptedByAccountId)
            throw new ArgumentException("Current atomic Deal and completed Booking audit required.");
        return new(){Id=id,BookingId=b.Id,CustomerId=b.CustomerId,BookingVersion=b.Version,Kind="Completed",
            Status=Booking.Completed,ActorAccountId=deal.AdoptedByAccountId,AtUtc=deal.AdoptedAtUtc,DealId=deal.Id,
            ConfirmedPrice=b.ConfirmedPrice,ConfirmedCurrencyCode=b.ConfirmedCurrencyCode,ReservationExpiresAtUtc=b.ReservationExpiresAtUtc};
    }
    public static BookingEvent Cancellation(Guid id, Booking b)
    {
        if (id == Guid.Empty || b.Status != Booking.Cancelled || b.IsActiveRequest || b.Version <= 1 || b.CancelledAtUtc is null ||
            b.CancelledByAccountId is null || b.CancelledFromStatus is not (Booking.PendingReview or Booking.Confirmed))
            throw new ArgumentException("Recorded actual owner cancellation required.");
        return new() { Id=id,BookingId=b.Id,CustomerId=b.CustomerId,BookingVersion=b.Version,Kind="Cancelled",Status=b.Status,
            ActorAccountId=b.CancelledByAccountId,AtUtc=b.CancelledAtUtc.Value,CancelledFromStatus=b.CancelledFromStatus,CancellationReason=b.CancellationReason,
            ConfirmedPrice=b.ConfirmedPrice,ConfirmedCurrencyCode=b.ConfirmedCurrencyCode,ReservationExpiresAtUtc=b.ReservationExpiresAtUtc };
    }
    public static BookingEvent Extended(BookingExtension extension, Booking b)
    {
        if (b.Status != Booking.Confirmed || b.LastExtensionId != extension.Id || b.Version != extension.BookingVersionAfter ||
            b.Id != extension.BookingId || b.CustomerId != extension.CustomerId || b.ReservationExpiresAtUtc != extension.NewReservationExpiresAtUtc)
            throw new ArgumentException("Current recorded extension and matching booking required.");
        return new() { Id=extension.Id,BookingId=b.Id,CustomerId=b.CustomerId,BookingVersion=b.Version,Kind="Extended",Status=b.Status,
            ActorAccountId=extension.RecorderAccountId,AtUtc=extension.RecordedAtUtc,ConfirmedPrice=b.ConfirmedPrice,
            ConfirmedCurrencyCode=b.ConfirmedCurrencyCode,ReservationExpiresAtUtc=extension.NewReservationExpiresAtUtc,
            PreviousReservationExpiresAtUtc=extension.PreviousReservationExpiresAtUtc,ExtensionId=extension.Id };
    }
    public static BookingEvent Decision(Guid id, Booking b)
    {
        if (id == Guid.Empty || b.Version <= 1 || b.Status is not (Booking.Confirmed or Booking.Rejected) ||
            (b.Status == Booking.Rejected && b.RejectionActorKind != "User")) throw new ArgumentException("Actual response state transition required.");
        return new() { Id = id, BookingId = b.Id, CustomerId = b.CustomerId, BookingVersion = b.Version,
            Kind = b.Status, Status = b.Status, ActorAccountId = b.Status == Booking.Confirmed ? b.ConfirmedByAccountId : b.RejectedByAccountId,
            AtUtc = (b.Status == Booking.Confirmed ? b.ConfirmedAtUtc : b.RejectedAtUtc)!.Value,
            ReasonCode = b.RejectionReason, ConfirmedPrice = b.ConfirmedPrice, ConfirmedCurrencyCode = b.ConfirmedCurrencyCode,
            ReservationExpiresAtUtc = b.ReservationExpiresAtUtc, DeveloperResponseId = id };
    }
    public static BookingEvent Expiry(Guid id, Booking b)
    {
        if (id == Guid.Empty || b.Status != Booking.Expired || b.Version <= 2 || b.ExpiredAtUtc is null || b.ReservationExpiresAtUtc is null)
            throw new ArgumentException("Recorded system reservation expiry required.");
        return new() { Id = id, BookingId = b.Id, CustomerId = b.CustomerId, BookingVersion = b.Version,
            Kind = "Expired", Status = Booking.Expired, ActorKind = "System", ActorAccountId = null, AtUtc = b.ExpiredAtUtc.Value,
            ReasonCode = "reservation-expired", EffectiveCutoffUtc = b.ReservationExpiresAtUtc, ConfirmedPrice = b.ConfirmedPrice,
            ConfirmedCurrencyCode = b.ConfirmedCurrencyCode, ReservationExpiresAtUtc = b.ReservationExpiresAtUtc };
    }
    public static BookingEvent Requested(Guid id, Booking booking)
    {
        if (id == Guid.Empty || booking.Status != Booking.PendingReview || booking.Version != 1 || !booking.IsActiveRequest)
            throw new ArgumentException("Initial requested booking required.");
        return new() { Id = id, BookingId = booking.Id, CustomerId = booking.CustomerId, BookingVersion = 1,
            Kind = "Requested", ActorAccountId = booking.RequestedByAccountId, AtUtc = booking.RequestedAtUtc, Status = Booking.PendingReview };
    }
    public static BookingEvent Timeout(Guid id, Booking booking)
    {
        if (id == Guid.Empty || booking.Status != Booking.Rejected || booking.IsActiveRequest || booking.Version <= 1 ||
            booking.RejectionReason != Booking.ResponseTimeout || booking.RejectionActorKind != "System" ||
            booking.RejectedByAccountId is not null || booking.RejectedAtUtc is null)
            throw new ArgumentException("Truthful committed system timeout audit required.");
        return new() { Id = id, BookingId = booking.Id, CustomerId = booking.CustomerId, BookingVersion = booking.Version,
            Kind = "ResponseTimedOut", ActorKind = "System", ActorAccountId = null, AtUtc = booking.RejectedAtUtc.Value,
            Status = Booking.Rejected, ReasonCode = Booking.ResponseTimeout, EffectiveCutoffUtc = DateTime.SpecifyKind(booking.ResponseDueAtUtc, DateTimeKind.Utc) };
    }
}
