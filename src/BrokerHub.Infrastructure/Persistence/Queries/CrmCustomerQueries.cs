using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class CrmCustomerQueries(ApplicationDbContext db) : ICrmCustomerQueries
{
    private const string ManagerRole = "SALESMANAGER";
    private const string EmployeeRole = "SALESEMPLOYEE";
    private IQueryable<Guid> RoleAccounts(string role) => from ar in db.UserRoles join r in db.Roles on ar.RoleId equals r.Id
        where r.NormalizedName == role select ar.UserId;
    private IQueryable<Customer> Scoped(Guid actor, bool managerOnly = false)
    {
        var managers = RoleAccounts(ManagerRole); var employees = RoleAccounts(EmployeeRole);
        return db.Customers.AsNoTracking().Where(c => db.Users.Any(u => u.Id == actor && u.IsActive && u.EmailConfirmed) &&
            (managers.Contains(actor) || (!managerOnly && employees.Contains(actor) &&
                db.Employees.Any(e => e.Id == c.AssignedEmployeeId && e.AccountId == actor))));
    }
    private Task<bool> ActorAllowed(Guid actor, bool managerOnly, CancellationToken ct)
    {
        var managers = RoleAccounts(ManagerRole); var employees = RoleAccounts(EmployeeRole);
        return db.Users.AnyAsync(u => u.Id == actor && u.IsActive && u.EmailConfirmed &&
            (managers.Contains(actor) || (!managerOnly && employees.Contains(actor))), ct);
    }
    public async Task<CrmResult<CrmPage<CrmCustomerDto>>> GetPageAsync(Guid actor, int page, int size, bool unassignedOnly, CancellationToken ct)
    {
        if (!await ActorAllowed(actor, unassignedOnly, ct)) return new(CrmStatus.Forbidden);
        var q = Scoped(actor, unassignedOnly).Where(c => !unassignedOnly || c.AssignedEmployeeId == null);
        var count = await q.CountAsync(ct); var rows = await q.OrderBy(c => c.FullName).ThenBy(c => c.Id).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(CrmStatus.Success, new(rows.Select(c => c.ToCrmDto()).ToArray(), page, size, count));
    }
    public async Task<CrmResult<CrmCustomerDto>> GetAsync(Guid actor, Guid id, CancellationToken ct)
    {
        if (!await ActorAllowed(actor, false, ct)) return new(CrmStatus.Forbidden);
        var row = await Scoped(actor).SingleOrDefaultAsync(c => c.Id == id, ct);
        return new(row is null ? CrmStatus.NotFound : CrmStatus.Success, row?.ToCrmDto());
    }
    private IQueryable<CustomerAssignment> History(Guid actor, Guid customerId) => from h in db.CustomerAssignments.AsNoTracking()
        join c in Scoped(actor) on h.CustomerId equals c.Id where c.Id == customerId select h;
    public async Task<CrmResult<CrmPage<CustomerAssignmentDto>>> GetHistoryAsync(Guid actor, Guid customerId, int page, int size, CancellationToken ct)
    {
        if (!await ActorAllowed(actor, false, ct)) return new(CrmStatus.Forbidden);
        if (!await Scoped(actor).AnyAsync(c => c.Id == customerId, ct)) return new(CrmStatus.NotFound);
        var q = History(actor, customerId); var count = await q.CountAsync(ct);
        var rows = await q.OrderBy(h => h.CustomerVersion).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(CrmStatus.Success, new(rows.Select(h => h.ToAssignmentDto()).ToArray(), page, size, count));
    }
    public async Task<CrmResult<CustomerAssignmentDto>> GetAssignmentAsync(Guid actor, Guid customerId, Guid assignmentId, CancellationToken ct)
    {
        if (!await ActorAllowed(actor, false, ct)) return new(CrmStatus.Forbidden);
        var row = await History(actor, customerId).SingleOrDefaultAsync(h => h.Id == assignmentId, ct);
        return new(row is null ? CrmStatus.NotFound : CrmStatus.Success, row?.ToAssignmentDto());
    }
    public async Task<CrmResult<CrmPage<AssignmentCandidateDto>>> GetCandidatesAsync(Guid actor, int page, int size, CancellationToken ct)
    {
        if (!await ActorAllowed(actor, true, ct)) return new(CrmStatus.Forbidden);
        var managers = RoleAccounts(ManagerRole); var employees = RoleAccounts(EmployeeRole);
        var q = db.Employees.AsNoTracking().Where(e => e.IsActive &&
            db.Users.Any(u => u.Id == actor && u.IsActive && u.EmailConfirmed) && managers.Contains(actor) &&
            db.Users.Any(u => u.Id == e.AccountId && u.IsActive && u.EmailConfirmed) && employees.Contains(e.AccountId));
        var count = await q.CountAsync(ct); var rows = await q.OrderBy(e => e.DisplayName).ThenBy(e => e.Id).Skip((page - 1) * size).Take(size)
            .Select(e => new AssignmentCandidateDto(e.Id, e.EmployeeNumber, e.DisplayName)).ToArrayAsync(ct);
        return new(CrmStatus.Success, new(rows, page, size, count));
    }
}
