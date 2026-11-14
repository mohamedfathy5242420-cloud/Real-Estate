using BrokerHub.Application.Authorization;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeRepository(ApplicationDbContext db) : IEmployeeRepository
{
    public Task<Employee?> GetTrackedAsync(Guid id, CancellationToken ct) => db.Employees.SingleOrDefaultAsync(e => e.Id == id, ct);
    public void Add(Employee employee) => db.Employees.Add(employee);
    public async Task CommitAsync(Employee employee, Guid actor, CancellationToken ct)
    {
        db.ChangeTracker.DetectChanges();
        var pending = db.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e => (Entry: e, Expected: e.CurrentValues.Clone())).ToArray();
        try
        {
            await db.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(db, async (context, token) =>
            {
                foreach (var accountId in new[] { actor, employee.AccountId }.Distinct().Order())
                {
                    var active = await context.Database.SqlQuery<int>($"""
                        SELECT CONVERT(int, CASE WHEN [IsActive]=1 THEN 1 ELSE 0 END) AS [Value]
                        FROM [Identity].[Accounts] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={accountId}
                        """).SingleOrDefaultAsync(token);
                    if (accountId == actor && active != 1) throw new EmployeeEligibilityException(true);
                    if (accountId == employee.AccountId && employee.IsActive && active != 1) throw new EmployeeEligibilityException(false);
                }
                var adminRole = AppRoles.SystemAdministrator.ToUpperInvariant();
                var actorAllowed = await context.Database.SqlQuery<int>($"""
                    SELECT COUNT(*) AS [Value] FROM [Identity].[AccountRoles] AS ar WITH (UPDLOCK,HOLDLOCK)
                    INNER JOIN [Identity].[Roles] AS r WITH (HOLDLOCK) ON r.[Id]=ar.[RoleId]
                    INNER JOIN [Identity].[Accounts] AS a WITH (HOLDLOCK) ON a.[Id]=ar.[UserId]
                    WHERE ar.[UserId]={actor} AND r.[NormalizedName]={adminRole} AND a.[EmailConfirmed]=1
                    """).SingleAsync(token);
                if (actorAllowed != 1) throw new EmployeeEligibilityException(true);
                if (employee.IsActive)
                {
                    var staffRole = AppRoles.SalesEmployee.ToUpperInvariant(); var managerRole = AppRoles.SalesManager.ToUpperInvariant();
                    var targetAllowed = await context.Database.SqlQuery<int>($"""
                        SELECT COUNT(*) AS [Value] FROM [Identity].[AccountRoles] AS ar WITH (UPDLOCK,HOLDLOCK)
                        INNER JOIN [Identity].[Roles] AS r WITH (HOLDLOCK) ON r.[Id]=ar.[RoleId]
                        WHERE ar.[UserId]={employee.AccountId} AND r.[NormalizedName] IN ({staffRole},{managerRole})
                        """).SingleAsync(token);
                    if (targetAllowed == 0) throw new EmployeeEligibilityException(false);
                }
                await context.SaveChangesAsync(acceptAllChangesOnSuccess: false, token); return true;
            }, async (_, token) =>
            {
                foreach (var (entry, expected) in pending)
                {
                    var persisted = await entry.GetDatabaseValuesAsync(token);
                    if (persisted is null || expected.Properties.Any(p => !Equals(expected[p], persisted[p]))) return false;
                }
                return pending.Length > 0;
            }, ct);
            db.ChangeTracker.AcceptAllChanges();
        }
        catch (DbUpdateConcurrencyException ex) { throw new OptimisticConcurrencyException("Employee changed before commit.", ex); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { throw new UniqueConstraintViolationException("Employee account or number already exists.", ex); }
    }
}
