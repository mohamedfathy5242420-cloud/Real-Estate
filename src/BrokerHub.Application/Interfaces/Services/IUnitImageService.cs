using BrokerHub.Application.DTOs.UnitImages;
namespace BrokerHub.Application.Interfaces.Services;
public interface IUnitImageService
{
    Task<UnitImageResult<UnitImageMutationDto>> UploadAsync(Guid unitId, UploadUnitImageDto request, CancellationToken cancellationToken);
    Task<UnitImageResult<UnitImageCollectionDto>> ListAsync(Guid unitId, CancellationToken cancellationToken);
    Task<UnitImageResult<UnitImageMutationDto>> UpdateAsync(Guid unitId, Guid imageId, UpdateUnitImageDto request, CancellationToken cancellationToken);
    Task<UnitImageResult<UnitImageContent>> ReadAsync(Guid unitId, Guid imageId, CancellationToken cancellationToken);
}
