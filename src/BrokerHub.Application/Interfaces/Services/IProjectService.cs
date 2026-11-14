using BrokerHub.Application.DTOs.Projects;

namespace BrokerHub.Application.Interfaces.Services;

public interface IProjectService
{
    Task<ProjectServiceResult<ProjectDto>> CreateAsync(
        CreateProjectDto request,
        CancellationToken cancellationToken);

    Task<ProjectServiceResult<ProjectDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ProjectServiceResult<IReadOnlyList<ProjectDto>>> ListAsync(
        Guid? developerId,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<ProjectServiceResult<ProjectDto>> UpdateAsync(
        Guid id,
        UpdateProjectDto request,
        CancellationToken cancellationToken);

    Task<ProjectServiceResult<ProjectDto>> DeactivateAsync(
        Guid id,
        long version,
        CancellationToken cancellationToken);

    Task<ProjectServiceResult<ProjectDto>> ReactivateAsync(
        Guid id,
        long version,
        CancellationToken cancellationToken);
}
