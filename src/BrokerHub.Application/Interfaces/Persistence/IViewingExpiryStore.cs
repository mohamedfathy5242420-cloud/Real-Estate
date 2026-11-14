using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;
public interface IViewingExpiryStore
{
    Task<IReadOnlyList<ViewingExpiryCandidate>> FindDueAsync(int limit, DateTime observedUtc, CancellationToken ct);
    Task<bool> TryExpireAsync(ViewingExpiryCandidate candidate, Func<Viewing, ViewingExpiryEffect?> stage, CancellationToken ct);
}
