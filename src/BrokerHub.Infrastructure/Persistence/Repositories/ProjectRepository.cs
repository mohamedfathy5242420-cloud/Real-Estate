using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;

internal sealed class ProjectRepository(ApplicationDbContext dbContext) : IProjectRepository
{
    public async Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        await dbContext.Projects.AddAsync(project, cancellationToken);
    }

    public Task<Project?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Projects.SingleOrDefaultAsync(
            project => project.Id == id,
            cancellationToken);

    public Task<bool> CodeExistsAsync(
        Guid developerId,
        string code,
        CancellationToken cancellationToken) =>
        dbContext.Projects.AnyAsync(
            project => project.DeveloperId == developerId && project.Code == code,
            cancellationToken);
}
