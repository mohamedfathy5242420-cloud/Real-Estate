using BrokerHub.Application.DTOs.Developers;

namespace BrokerHub.Application.Interfaces.Services;

public interface IDeveloperService
{
    Task<DeveloperServiceResult<DeveloperDto>> CreateAsync(
        CreateDeveloperDto request,
        CancellationToken cancellationToken);

    Task<DeveloperServiceResult<DeveloperDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<DeveloperServiceResult<IReadOnlyList<DeveloperDto>>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<DeveloperServiceResult<DeveloperDto>> UpdateAsync(
        Guid id,
        UpdateDeveloperDto request,
        CancellationToken cancellationToken);

    Task<DeveloperServiceResult<DeveloperDto>> DeactivateAsync(
        Guid id,
        long version,
        CancellationToken cancellationToken);

    Task<DeveloperServiceResult<DeveloperDto>> ReactivateAsync(
        Guid id,
        long version,
        CancellationToken cancellationToken);
}
