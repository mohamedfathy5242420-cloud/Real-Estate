using BrokerHub.Application.DTOs.Developers;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IDeveloperQueryService
{
    Task<DeveloperDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DeveloperDto>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken);
}
