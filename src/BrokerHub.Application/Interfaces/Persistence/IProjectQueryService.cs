using BrokerHub.Application.DTOs.Projects;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IProjectQueryService
{
    Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectDto>> ListAsync(
        Guid? developerId,
        bool includeInactive,
        CancellationToken cancellationToken);
}
