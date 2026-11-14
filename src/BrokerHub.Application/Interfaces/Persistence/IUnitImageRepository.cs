using BrokerHub.Domain.Entities;
namespace BrokerHub.Application.Interfaces.Persistence;
public interface IUnitImageRepository
{
    Task AddAsync(UnitImage image, CancellationToken cancellationToken);
    Task<UnitImage?> GetTrackedAsync(Guid unitId, Guid imageId, CancellationToken cancellationToken);
    // Fresh read after a failed/uncertain save. null means unknown, so compensation MUST retain the file.
    Task<bool?> IsStorageKeyReferencedAsync(string key, CancellationToken cancellationToken);
}
