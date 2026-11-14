using BrokerHub.Application.DTOs.Requests;

namespace BrokerHub.Application.Interfaces.Persistence;
// Catalog locks only inside the caller's command transaction; never saves or commits.
public interface IRequestUnitRepository
{
    Task<RequestUnitIdentity?> ResolveIdentityAsync(Guid unitId, CancellationToken ct);
    Task<RequestUnitContext?> LockAsync(RequestUnitIdentity identity, CancellationToken ct);
}
