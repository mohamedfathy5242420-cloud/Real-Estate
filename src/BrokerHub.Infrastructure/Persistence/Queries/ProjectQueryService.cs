using BrokerHub.Application.DTOs.Projects;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class ProjectQueryService(ApplicationDbContext dbContext) : IProjectQueryService
{
    public Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Id == id)
            .Select(ProjectMappings.Projection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectDto>> ListAsync(
        Guid? developerId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Projects.AsNoTracking();
        if (developerId.HasValue)
        {
            query = query.Where(project => project.DeveloperId == developerId.Value);
        }

        if (!includeInactive)
        {
            query = query.Where(project => project.IsActive);
        }

        return await query
            .OrderBy(project => project.Name)
            .ThenBy(project => project.Code)
            .Select(ProjectMappings.Projection)
            .ToListAsync(cancellationToken);
    }
}
