using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken);

    Task<Project?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(
        Guid developerId,
        string code,
        CancellationToken cancellationToken);
}
