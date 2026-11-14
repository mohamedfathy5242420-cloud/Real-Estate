using BrokerHub.Application.DTOs.Units;

namespace BrokerHub.Application.Interfaces.Services;

public interface IUnitService
{
    Task<UnitServiceResult<UnitDto>> CreateAsync(CreateUnitDto request, CancellationToken cancellationToken);
    Task<UnitServiceResult<UnitDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UnitServiceResult<IReadOnlyList<UnitDto>>> ListAsync(Guid? projectId, CancellationToken cancellationToken);
    Task<UnitServiceResult<UnitDto>> UpdateAsync(Guid id, UpdateUnitDto request, CancellationToken cancellationToken);
}
