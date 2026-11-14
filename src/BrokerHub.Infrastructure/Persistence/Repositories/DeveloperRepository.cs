using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;

internal sealed class DeveloperRepository(ApplicationDbContext dbContext) : IDeveloperRepository
{
    public async Task AddAsync(Developer developer, CancellationToken cancellationToken)
    {
        await dbContext.Developers.AddAsync(developer, cancellationToken);
    }

    public Task<Developer?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Developers.SingleOrDefaultAsync(
            developer => developer.Id == id,
            cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        dbContext.Developers.AnyAsync(
            developer => developer.Code == code,
            cancellationToken);
}
