using System.Text.Json;
using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Serialization;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;
public sealed class ViewingExpiryService(IViewingExpiryStore store, TimeProvider clock) : IViewingExpiryService
{
    public async Task<ViewingExpiryBatch> ExpireDueAsync(int limit, CancellationToken ct)
    {
        if (limit is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(limit));
        var candidates = await store.FindDueAsync(limit, clock.GetUtcNow().UtcDateTime, ct); var expired = 0;
        foreach (var candidate in candidates) { ct.ThrowIfCancellationRequested(); if (await TryExpireAsync(candidate, ct)) expired++; }
        return new(candidates.Count, expired);
    }
    public Task<bool> TryExpireAsync(ViewingExpiryCandidate candidate, CancellationToken ct) => store.TryExpireAsync(candidate, v =>
    {
        var now = clock.GetUtcNow().UtcDateTime;
        if (!v.TryExpire(candidate.Version, now)) return null;
        var history = ViewingEvent.Expiry(Guid.NewGuid(), v);
        var payload = CanonicalCommandJson.Canonicalize(JsonSerializer.SerializeToElement(new { viewingId = v.Id,
            customerId = v.CustomerId, unitId = v.UnitId, cycleStartedAtUtc = DateTime.SpecifyKind(v.PendingCycleStartedAtUtc, DateTimeKind.Utc),
            effectiveCutoffUtc = history.EffectiveCutoffUtc, expiredAtUtc = v.ExpiredAtUtc, previousStatus = v.ExpiredFromStatus, version = v.Version }));
        return new(history, SystemOutboxMessage.Record(history.Id, "System.ViewingExpiry", "Viewing.Expired", 1,
            v.CustomerId, v.Id, v.Version, payload, now, now));
    }, ct);
}
