using BrokerHub.Application.DTOs.Developers;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class DeveloperQueryService(ApplicationDbContext dbContext) : IDeveloperQueryService
{
    public Task<DeveloperDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Developers
            .AsNoTracking()
            .Where(developer => developer.Id == id)
            .Select(DeveloperMappings.Projection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<DeveloperDto>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Developers.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(developer => developer.IsActive);
        }

        return await query
            .OrderBy(developer => developer.Name)
            .ThenBy(developer => developer.Code)
            .Select(DeveloperMappings.Projection)
            .ToListAsync(cancellationToken);
    }
}
