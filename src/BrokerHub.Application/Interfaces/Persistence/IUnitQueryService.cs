using BrokerHub.Application.DTOs.Units;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IUnitQueryService
{
    Task<UnitDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<UnitDto>> ListAsync(Guid? projectId, CancellationToken cancellationToken);
}
