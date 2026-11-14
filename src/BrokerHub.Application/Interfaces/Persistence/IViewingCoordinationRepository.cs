using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;
// Caller owns one command transaction/current Customer lock; stages updates without Save/Commit.
public interface IViewingCoordinationRepository
{
    Task<Viewing?> LockAsync(Guid customerId, Guid id, CancellationToken ct);
    Task<Guid?> LockResponsibleAccountAsync(Guid employeeId, CancellationToken ct);
    Task<bool> LockResourcesAsync(Guid unitId, Guid employeeId, Guid accountId, CancellationToken ct);
    Task<bool> ConflictsAsync(Guid unitId, Guid employeeId, DateTime startUtc, DateTime endUtc, CancellationToken ct);
}
