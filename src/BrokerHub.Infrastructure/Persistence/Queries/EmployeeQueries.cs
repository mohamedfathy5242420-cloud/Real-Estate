using BrokerHub.Application.DTOs.Employees;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class EmployeeQueries(ApplicationDbContext db) : IEmployeeQueries
{
    public async Task<EmployeeProfileDto?> GetAsync(Guid id, CancellationToken ct) =>
        (await db.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, ct))?.ToProfileDto();
    public async Task<EmployeePage<EmployeeProfileDto>> GetPageAsync(int page, int size, bool? isActive, CancellationToken ct)
    {
        var q = db.Employees.AsNoTracking().Where(e => !isActive.HasValue || e.IsActive == isActive.Value);
        var count = await q.CountAsync(ct);
        var rows = await q.OrderBy(e => e.EmployeeNumber).ThenBy(e => e.Id).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(rows.Select(e => e.ToProfileDto()).ToArray(), page, size, count);
    }
    public async Task<EmployeePage<EmployeeRevisionDto>?> GetHistoryAsync(Guid id, int page, int size, CancellationToken ct)
    {
        if (!await db.Employees.AnyAsync(e => e.Id == id, ct)) return null;
        var q = db.EmployeeRevisions.AsNoTracking().Where(r => r.EmployeeId == id);
        var count = await q.CountAsync(ct);
        var rows = await q.OrderBy(r => r.Version).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(rows.Select(r => r.ToRevisionDto()).ToArray(), page, size, count);
    }
}
