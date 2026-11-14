namespace BrokerHub.Application.DTOs.Bookings;
public sealed record BookingExpiryCandidate(Guid Id, Guid CustomerId, Guid UnitId, long Version, string Status, DateTime EffectiveCutoffUtc);
public sealed record BookingExpiryBatch(int Inspected, int Expired);
