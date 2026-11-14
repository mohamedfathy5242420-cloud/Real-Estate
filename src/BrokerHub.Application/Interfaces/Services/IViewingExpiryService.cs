using BrokerHub.Application.DTOs.Viewings;

namespace BrokerHub.Application.Interfaces.Services;
// Trusted internal system workflow, deliberately no public controller or human impersonation.
public interface IViewingExpiryService
{
    Task<ViewingExpiryBatch> ExpireDueAsync(int limit, CancellationToken ct);
    Task<bool> TryExpireAsync(ViewingExpiryCandidate candidate, CancellationToken ct);
}
