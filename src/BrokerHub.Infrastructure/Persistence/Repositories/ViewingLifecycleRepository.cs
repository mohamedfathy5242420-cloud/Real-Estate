using BrokerHub.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;
internal sealed class ViewingLifecycleRepository(ApplicationDbContext db) : IViewingLifecycleRepository
{
    public async Task<bool> LockReleasedResourcesAsync(Guid unitId, Guid employeeId, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Viewing lifecycle requires the command transaction.");
        if (await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Catalog].[Units] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={unitId}").SingleAsync(ct) != 1) return false;
        return await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Crm].[Employees] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={employeeId}").SingleAsync(ct) == 1;
    }
}
