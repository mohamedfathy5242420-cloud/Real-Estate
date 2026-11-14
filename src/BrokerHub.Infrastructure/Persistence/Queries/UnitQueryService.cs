using BrokerHub.Application.DTOs.Units;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class UnitQueryService(ApplicationDbContext dbContext) : IUnitQueryService
{
    public Task<UnitDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Units.AsNoTracking().Where(unit => unit.Id == id).Select(UnitMappings.Projection)
            .SingleOrDefaultAsync(cancellationToken);
    public async Task<IReadOnlyList<UnitDto>> ListAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        var query = dbContext.Units.AsNoTracking();
        if (projectId.HasValue) query = query.Where(unit => unit.ProjectId == projectId.Value);
        return await query.OrderBy(unit => unit.Code).ThenBy(unit => unit.Id)
            .Select(UnitMappings.Projection).ToListAsync(cancellationToken);
    }
}
