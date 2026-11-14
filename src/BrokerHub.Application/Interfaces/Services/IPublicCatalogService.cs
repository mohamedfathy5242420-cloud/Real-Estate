using BrokerHub.Application.DTOs.Catalog;
using BrokerHub.Application.DTOs.UnitImages;
namespace BrokerHub.Application.Interfaces.Services;
public interface IPublicCatalogService
{
    Task<PublicCatalogResult<PublicCatalogPageDto>> ListAsync(CatalogSearchDto search, CancellationToken ct);
    Task<PublicCatalogResult<PublicUnitDetailDto>> DetailAsync(Guid id, CancellationToken ct);
    Task<PublicCatalogResult<UnitImageContent>> ImageAsync(Guid id, Guid imageId, CancellationToken ct);
}
