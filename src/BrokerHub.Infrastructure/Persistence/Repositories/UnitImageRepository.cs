using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace BrokerHub.Infrastructure.Persistence.Repositories;
internal sealed class UnitImageRepository(ApplicationDbContext context,
    DbContextOptions<ApplicationDbContext> options, ILogger<UnitImageRepository> logger) : IUnitImageRepository
{
    public async Task AddAsync(UnitImage image, CancellationToken cancellationToken) =>
        await context.UnitImages.AddAsync(image, cancellationToken);
    public Task<UnitImage?> GetTrackedAsync(Guid unitId, Guid imageId, CancellationToken cancellationToken) =>
        context.UnitImages.SingleOrDefaultAsync(image => image.UnitId == unitId && image.Id == imageId, cancellationToken);
    public async Task<bool?> IsStorageKeyReferencedAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await using var fresh = new ApplicationDbContext(options);
            return await fresh.UnitImages.AsNoTracking().AnyAsync(image => image.StorageKey == key, cancellationToken);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            logger.LogWarning(e, "Cannot resolve SQL reference for generated image {Key}; retain file for reconciliation.", key);
            return null;
        }
    }
}
