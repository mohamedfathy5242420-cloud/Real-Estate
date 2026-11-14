using BrokerHub.Application.DTOs.Catalog;
namespace BrokerHub.Application.Interfaces.Persistence;

public interface IPublicCatalogQueries
{
    Task<PublicCatalogPageDto> ListAsync(CatalogSearchDto search, DateTimeOffset at, CancellationToken ct);
    Task<PublicUnitDetailDto?> DetailAsync(Guid id, DateTimeOffset at, CancellationToken ct);
    // Internal storage reference; never part of a public response.
    Task<string?> ImageKeyAsync(Guid unitId, Guid imageId, DateTimeOffset at, CancellationToken ct);
}
