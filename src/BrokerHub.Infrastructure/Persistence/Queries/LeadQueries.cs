using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.DTOs.Leads;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;
internal sealed class LeadQueries(ApplicationDbContext db) : ILeadQueries
{
    private IQueryable<Guid> AccountsIn(string role) => from ar in db.UserRoles join r in db.Roles on ar.RoleId equals r.Id where r.NormalizedName == role select ar.UserId;
    private Task<bool> ActorAllowed(Guid actor, CancellationToken ct)
    {
        var managers = AccountsIn("SALESMANAGER"); var employees = AccountsIn("SALESEMPLOYEE");
        return db.Users.AnyAsync(u => u.Id == actor && u.IsActive && u.EmailConfirmed && (managers.Contains(actor) || employees.Contains(actor)), ct);
    }
    private IQueryable<Customer> Customers(Guid actor)
    {
        var managers = AccountsIn("SALESMANAGER"); var employees = AccountsIn("SALESEMPLOYEE");
        return db.Customers.AsNoTracking().Where(c => db.Users.Any(u => u.Id == actor && u.IsActive && u.EmailConfirmed) &&
            (managers.Contains(actor) || (employees.Contains(actor) && db.Employees.Any(e => e.Id == c.AssignedEmployeeId && e.AccountId == actor))));
    }
    private IQueryable<Lead> Scoped(Guid actor, Guid customerId) => from l in db.Leads.AsNoTracking()
        join c in Customers(actor) on l.CustomerId equals c.Id where c.Id == customerId select l;
    private IQueryable<LeadEvent> Events(Guid actor, Guid customerId, Guid id) => from e in db.LeadEvents.AsNoTracking()
        join l in Scoped(actor, customerId) on e.LeadId equals l.Id where l.Id == id && e.CustomerId == customerId select e;
    public async Task<CrmResult<CrmPage<LeadDto>>> ListAsync(Guid actor, Guid customerId, int page, int size, string? status, CancellationToken ct)
    {
        if (!await ActorAllowed(actor, ct)) return new(CrmStatus.Forbidden);
        if (!await Customers(actor).AnyAsync(c => c.Id == customerId, ct)) return new(CrmStatus.NotFound);
        var q = Scoped(actor, customerId).Where(l => status == null || l.Status == status); var count = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(l => l.OpenedAtUtc).ThenBy(l => l.Id).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(CrmStatus.Success, new(rows.Select(l => l.ToDto()).ToArray(), page, size, count));
    }
    public async Task<CrmResult<LeadDto>> GetAsync(Guid actor, Guid customerId, Guid id, CancellationToken ct)
    {
        if (!await ActorAllowed(actor, ct)) return new(CrmStatus.Forbidden);
        var l = await Scoped(actor, customerId).SingleOrDefaultAsync(l => l.Id == id, ct);
        return new(l is null ? CrmStatus.NotFound : CrmStatus.Success, l?.ToDto());
    }
    public async Task<CrmResult<CrmPage<LeadEventDto>>> HistoryAsync(Guid actor, Guid customerId, Guid id, int page, int size, CancellationToken ct)
    {
        if (!await ActorAllowed(actor, ct)) return new(CrmStatus.Forbidden);
        if (!await Scoped(actor, customerId).AnyAsync(l => l.Id == id, ct)) return new(CrmStatus.NotFound);
        var q = Events(actor, customerId, id); var count = await q.CountAsync(ct);
        var rows = await q.OrderBy(e => e.LeadVersion).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(CrmStatus.Success, new(rows.Select(e => e.ToDto()).ToArray(), page, size, count));
    }
    public async Task<CrmResult<LeadEventDto>> EventAsync(Guid actor, Guid customerId, Guid id, Guid eventId, CancellationToken ct)
    {
        if (!await ActorAllowed(actor, ct)) return new(CrmStatus.Forbidden);
        var e = await Events(actor, customerId, id).SingleOrDefaultAsync(e => e.Id == eventId, ct);
        return new(e is null ? CrmStatus.NotFound : CrmStatus.Success, e?.ToDto());
    }
}
