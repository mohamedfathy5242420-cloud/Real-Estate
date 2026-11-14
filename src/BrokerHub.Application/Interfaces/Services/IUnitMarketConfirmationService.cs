using BrokerHub.Application.DTOs.UnitMarketConfirmations;
using BrokerHub.Application.DTOs.Units;
namespace BrokerHub.Application.Interfaces.Services;
public interface IUnitMarketConfirmationService
{
    Task<UnitServiceResult<UnitMarketConfirmationMutationDto>> RecordAsync(Guid unitId, RecordUnitMarketConfirmationDto request, CancellationToken ct);
    Task<UnitServiceResult<IReadOnlyList<UnitMarketConfirmationDto>>> ListAsync(Guid unitId, int skip, int take, CancellationToken ct);
}
