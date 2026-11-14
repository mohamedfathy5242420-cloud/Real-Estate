using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.DTOs.FollowUp;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;
internal sealed class CrmFollowUpQueries(ApplicationDbContext db) : ICrmFollowUpQueries
{
    private IQueryable<Guid> AccountsIn(string role) => from ar in db.UserRoles join r in db.Roles on ar.RoleId equals r.Id where r.NormalizedName == role select ar.UserId;
    private Task<bool> Allowed(Guid actor, CancellationToken ct)
    {
        var managers = AccountsIn("SALESMANAGER"); var employees = AccountsIn("SALESEMPLOYEE");
        return db.Users.AnyAsync(u => u.Id == actor && u.IsActive && u.EmailConfirmed && (managers.Contains(actor) || employees.Contains(actor)), ct);
    }
    private IQueryable<Customer> Scoped(Guid actor)
    {
        var managers = AccountsIn("SALESMANAGER"); var employees = AccountsIn("SALESEMPLOYEE");
        return db.Customers.AsNoTracking().Where(c => db.Users.Any(u => u.Id == actor && u.IsActive && u.EmailConfirmed) &&
            (managers.Contains(actor) || (employees.Contains(actor) && db.Employees.Any(e => e.Id == c.AssignedEmployeeId && e.AccountId == actor))));
    }
    private IQueryable<SalesNote> Notes(Guid actor, Guid customerId) => from n in db.SalesNotes.AsNoTracking()
        join c in Scoped(actor) on n.CustomerId equals c.Id where c.Id == customerId select n;
    private IQueryable<CustomerNeedsChange> Changes(Guid actor, Guid customerId) => from h in db.CustomerNeedsChanges.AsNoTracking()
        join c in Scoped(actor) on h.CustomerId equals c.Id where c.Id == customerId select h;
    public async Task<CrmResult<CrmPage<CrmCustomerDto>>> QueueAsync(Guid actor, int page, int size, bool? hasRequirements, bool? hasNotes, CancellationToken ct)
    {
        if (!await Allowed(actor, ct)) return new(CrmStatus.Forbidden);
        var q = Scoped(actor);
        if (hasRequirements is { } requirements) q = q.Where(c => (c.Requirements != null) == requirements);
        if (hasNotes is { } notes) q = q.Where(c => db.SalesNotes.Any(n => n.CustomerId == c.Id) == notes);
        var count = await q.CountAsync(ct); var rows = await q.OrderByDescending(c => c.UpdatedAtUtc ?? c.CreatedAtUtc).ThenBy(c => c.Id).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(CrmStatus.Success, new(rows.Select(c => c.ToCrmDto()).ToArray(), page, size, count));
    }
    public async Task<CrmResult<CrmPage<SalesNoteDto>>> NotesAsync(Guid actor, Guid customerId, Guid? leadId, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor, ct)) return new(CrmStatus.Forbidden);
        if (!await Scoped(actor).AnyAsync(c => c.Id == customerId, ct)) return new(CrmStatus.NotFound);
        var q = Notes(actor, customerId); if (leadId is { } lead) q = q.Where(n => n.LeadId == lead);
        var count = await q.CountAsync(ct); var rows = await q.OrderByDescending(n => n.CreatedAtUtc).ThenBy(n => n.Id).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(CrmStatus.Success, new(rows.Select(n => n.ToDto()).ToArray(), page, size, count));
    }
    public async Task<CrmResult<SalesNoteDto>> NoteAsync(Guid actor, Guid customerId, Guid noteId, CancellationToken ct)
    {
        if (!await Allowed(actor, ct)) return new(CrmStatus.Forbidden);
        var row = await Notes(actor, customerId).SingleOrDefaultAsync(n => n.Id == noteId, ct);
        return new(row is null ? CrmStatus.NotFound : CrmStatus.Success, row?.ToDto());
    }
    public async Task<CrmResult<CrmPage<CustomerNeedsChangeDto>>> NeedsHistoryAsync(Guid actor, Guid customerId, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor, ct)) return new(CrmStatus.Forbidden);
        if (!await Scoped(actor).AnyAsync(c => c.Id == customerId, ct)) return new(CrmStatus.NotFound);
        var q = Changes(actor, customerId); var count = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(h => h.CustomerVersion).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(CrmStatus.Success, new(rows.Select(h => h.ToDto()).ToArray(), page, size, count));
    }
    public async Task<CrmResult<CustomerNeedsChangeDto>> NeedsChangeAsync(Guid actor, Guid customerId, Guid changeId, CancellationToken ct)
    {
        if (!await Allowed(actor, ct)) return new(CrmStatus.Forbidden);
        var row = await Changes(actor, customerId).SingleOrDefaultAsync(h => h.Id == changeId, ct);
        return new(row is null ? CrmStatus.NotFound : CrmStatus.Success, row?.ToDto());
    }
}
