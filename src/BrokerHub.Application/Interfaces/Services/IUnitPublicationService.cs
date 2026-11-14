using BrokerHub.Application.DTOs.Units;
using BrokerHub.Application.DTOs.UnitPublications;
namespace BrokerHub.Application.Interfaces.Services;
public interface IUnitPublicationService
{
    Task<UnitServiceResult<UnitPublicationMutationDto>> ChangeAsync(Guid unitId, bool publish, ChangeUnitPublicationDto request, CancellationToken ct);
}
