using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.DTOs.Viewings;
public sealed record ViewingExpiryCandidate(Guid Id, Guid CustomerId, long Version, string Status,
    DateTime CycleStartedAtUtc, DateTime EffectiveCutoffUtc);
public sealed record ViewingExpiryEffect(ViewingEvent History, SystemOutboxMessage Event);
public sealed record ViewingExpiryBatch(int Inspected, int Expired);
