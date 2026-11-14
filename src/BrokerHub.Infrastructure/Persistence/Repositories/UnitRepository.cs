using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;

internal sealed class UnitRepository(ApplicationDbContext dbContext) : IUnitRepository
{
    public async Task AddAsync(Unit unit, CancellationToken cancellationToken) =>
        await dbContext.Units.AddAsync(unit, cancellationToken);
    public Task<Unit?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Units.SingleOrDefaultAsync(unit => unit.Id == id, cancellationToken);
    public Task<Unit?> LockAsync(Guid id,CancellationToken cancellationToken)
    {
        if(dbContext.Database.CurrentTransaction is null)throw new InvalidOperationException("Unit lock requires caller transaction.");
        return dbContext.Units.FromSqlInterpolated($"SELECT * FROM [Catalog].[Units] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").SingleOrDefaultAsync(cancellationToken);
    }
    public Task<bool> CodeExistsAsync(Guid projectId, string code, CancellationToken cancellationToken) =>
        dbContext.Units.AnyAsync(unit => unit.ProjectId == projectId && unit.Code == code, cancellationToken);
}
