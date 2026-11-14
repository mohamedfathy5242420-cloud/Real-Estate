using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IUnitRepository
{
    Task AddAsync(Unit unit, CancellationToken cancellationToken);
    Task<Unit?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Unit?> LockAsync(Guid id,CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(Guid projectId, string code, CancellationToken cancellationToken);
}
