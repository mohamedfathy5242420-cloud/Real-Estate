using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IDeveloperRepository
{
    Task AddAsync(Developer developer, CancellationToken cancellationToken);

    Task<Developer?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);
}
