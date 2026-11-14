using BrokerHub.Application.DTOs.UnitImages;
namespace BrokerHub.Application.Interfaces.Persistence;
public interface IUnitImageQueryService
{
    Task<UnitImageCollectionDto?> ListAsync(Guid unitId, CancellationToken cancellationToken);
    Task<UnitImageFileReference?> GetImageAsync(Guid unitId, Guid imageId, CancellationToken cancellationToken);
}
