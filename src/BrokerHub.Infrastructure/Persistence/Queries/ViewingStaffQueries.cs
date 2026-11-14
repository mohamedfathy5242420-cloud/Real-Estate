using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;
internal sealed class ViewingStaffQueries(ApplicationDbContext db, TimeProvider clock) : IViewingStaffQueries
{
    private IQueryable<Guid> AccountsIn(string role) => from ar in db.UserRoles join r in db.Roles on ar.RoleId equals r.Id where r.NormalizedName == role select ar.UserId;
    private Task<bool> Allowed(Guid actor, CancellationToken ct)
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
    private IQueryable<Viewing> Scoped(Guid actor) => from v in db.Viewings.AsNoTracking() join c in Customers(actor) on v.CustomerId equals c.Id select v;
    private IQueryable<ViewingEvent> Events(Guid actor, Guid customer, Guid id) => from e in db.ViewingEvents.AsNoTracking()
        join v in Scoped(actor) on new { e.ViewingId, e.CustomerId } equals new { ViewingId = v.Id, v.CustomerId }
        where v.Id == id && v.CustomerId == customer select e;
    public async Task<ViewingResult<ViewingPage<StaffViewingDto>>> ListAsync(Guid actor, Guid? customerId, int page, int size, string? status, CancellationToken ct)
    {
        if (!await Allowed(actor, ct)) return new(CommandStatus.Forbidden);
        if (customerId is { } customer && !await Customers(actor).AnyAsync(c => c.Id == customer, ct)) return new(CommandStatus.NotFound);
        var observedUtc = clock.GetUtcNow().UtcDateTime;
        var q = Scoped(actor).Where(v => customerId == null || v.CustomerId == customerId);
        if (status == Viewing.Expired) q = q.Where(v => v.Status == Viewing.Expired ||
            (v.Status == Viewing.PendingConfirmation && (v.CurrentProposedStartUtc <= observedUtc || v.PendingCycleStartedAtUtc.AddDays(7) <= observedUtc)) ||
            (v.Status == Viewing.AwaitingCustomerAcceptance && v.CurrentProposedStartUtc <= observedUtc));
        else if (status is Viewing.PendingConfirmation or Viewing.AwaitingCustomerAcceptance) q = q.Where(v => v.Status == status && v.CurrentProposedStartUtc > observedUtc &&
            (status == Viewing.AwaitingCustomerAcceptance || v.PendingCycleStartedAtUtc.AddDays(7) > observedUtc));
        else if (status is not null) q = q.Where(v => v.Status == status);
        var total = await q.CountAsync(ct); var rows = await q.OrderBy(v => v.CurrentProposedStartUtc).ThenBy(v => v.Id).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success, new(rows.Select(v => v.ToStaffDto(observedUtc)).ToArray(), total, page, size));
    }
    public async Task<ViewingResult<StaffViewingDto>> GetAsync(Guid actor, Guid customerId, Guid id, CancellationToken ct)
    {
        if (!await Allowed(actor, ct)) return new(CommandStatus.Forbidden);
        var observedUtc = clock.GetUtcNow().UtcDateTime;
        var v = await Scoped(actor).SingleOrDefaultAsync(v => v.CustomerId == customerId && v.Id == id, ct);
        return new(v is null ? CommandStatus.NotFound : CommandStatus.Success, v?.ToStaffDto(observedUtc));
    }
    public async Task<ViewingResult<ViewingPage<StaffViewingEventDto>>> HistoryAsync(Guid actor, Guid customerId, Guid id, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor, ct)) return new(CommandStatus.Forbidden);
        if (!await Scoped(actor).AnyAsync(v => v.CustomerId == customerId && v.Id == id, ct)) return new(CommandStatus.NotFound);
        var q = Events(actor, customerId, id); var total = await q.CountAsync(ct);
        var rows = await q.OrderBy(e => e.ViewingVersion).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success, new(rows.Select(e => e.ToStaffDto()).ToArray(), total, page, size));
    }
    public async Task<ViewingResult<StaffViewingEventDto>> EventAsync(Guid actor, Guid customerId, Guid id, Guid eventId, CancellationToken ct)
    {
        if (!await Allowed(actor, ct)) return new(CommandStatus.Forbidden);
        var e = await Events(actor, customerId, id).SingleOrDefaultAsync(e => e.Id == eventId, ct);
        return new(e is null ? CommandStatus.NotFound : CommandStatus.Success, e?.ToStaffDto());
    }
}
