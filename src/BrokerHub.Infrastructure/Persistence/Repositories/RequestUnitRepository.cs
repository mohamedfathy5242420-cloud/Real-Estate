using BrokerHub.Application.DTOs.Requests;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;
internal sealed class RequestUnitRepository(ApplicationDbContext db) : IRequestUnitRepository
{
    public Task<RequestUnitIdentity?> ResolveIdentityAsync(Guid unitId, CancellationToken ct)
    {
        RequireTransaction();
        return (from u in db.Units.AsNoTracking() join p in db.Projects.AsNoTracking() on u.ProjectId equals p.Id
            where u.Id == unitId select new RequestUnitIdentity(u.Id, p.Id, p.DeveloperId)).SingleOrDefaultAsync(ct);
    }
    public async Task<RequestUnitContext?> LockAsync(RequestUnitIdentity identity, CancellationToken ct)
    {
        RequireTransaction();
        var developer = await db.Developers.FromSqlInterpolated($"""
            SELECT * FROM [Catalog].[Developers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={identity.DeveloperId}
            """).AsNoTracking().SingleOrDefaultAsync(ct);
        if (developer is null) return null;
        var project = await db.Projects.FromSqlInterpolated($"""
            SELECT * FROM [Catalog].[Projects] WITH (HOLDLOCK) WHERE [Id]={identity.ProjectId} AND [DeveloperId]={identity.DeveloperId}
            """).AsNoTracking().SingleOrDefaultAsync(ct);
        if (project is null) return null;
        var unit = await db.Units.FromSqlInterpolated($"""
            SELECT * FROM [Catalog].[Units] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={identity.UnitId} AND [ProjectId]={identity.ProjectId}
            """).AsNoTracking().SingleOrDefaultAsync(ct);
        var sold = unit is not null && await db.Deals.AsNoTracking().AnyAsync(d => d.UnitId == unit.Id,ct);
        return unit is null ? null : new(unit.Id, project.Id, developer.Id, unit.PublicationStatus == Unit.Published && !sold,
            developer.IsActive, project.IsActive, project.TimeZoneId);
    }
    private void RequireTransaction()
    { if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Request catalog locks require the command transaction."); }
}
