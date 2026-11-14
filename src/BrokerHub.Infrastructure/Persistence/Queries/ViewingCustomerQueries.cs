using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;
internal sealed class ViewingCustomerQueries(ApplicationDbContext db, TimeProvider clock) : IViewingCustomerQueries
{
    private IQueryable<Guid> Accounts => from ar in db.UserRoles join r in db.Roles on ar.RoleId equals r.Id
        join a in db.Users on ar.UserId equals a.Id where r.NormalizedName == "CUSTOMER" && a.IsActive && a.EmailConfirmed select a.Id;
    private IQueryable<Customer> Owned(Guid actor) => db.Customers.AsNoTracking().Where(c => c.AccountId == actor && Accounts.Contains(actor));
    private IQueryable<Viewing> Scoped(Guid actor) => from v in db.Viewings.AsNoTracking() join c in Owned(actor) on v.CustomerId equals c.Id select v;
    private IQueryable<ViewingEvent> Events(Guid actor, Guid id) => from e in db.ViewingEvents.AsNoTracking()
        join v in Scoped(actor) on new { e.ViewingId, e.CustomerId } equals new { ViewingId = v.Id, v.CustomerId }
        where v.Id == id select e;
    public async Task<ViewingResult<ViewingPage<CustomerViewingDto>>> MineAsync(Guid actor, int page, int size, CancellationToken ct)
    {
        if (!await Accounts.ContainsAsync(actor, ct)) return new(CommandStatus.Forbidden);
        if (!await Owned(actor).AnyAsync(ct)) return new(CommandStatus.NotFound);
        var observedUtc = clock.GetUtcNow().UtcDateTime;
        var q = Scoped(actor); var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(v => v.RequestedAtUtc).ThenBy(v => v.Id).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success, new(rows.Select(v => v.ToCustomerDto(observedUtc)).ToArray(), total, page, size));
    }
    public async Task<ViewingResult<CustomerViewingDto>> GetAsync(Guid actor, Guid id, CancellationToken ct)
    {
        if (!await Accounts.ContainsAsync(actor, ct)) return new(CommandStatus.Forbidden);
        var observedUtc = clock.GetUtcNow().UtcDateTime;
        var v = await Scoped(actor).SingleOrDefaultAsync(v => v.Id == id, ct);
        return new(v is null ? CommandStatus.NotFound : CommandStatus.Success, v?.ToCustomerDto(observedUtc));
    }
    public async Task<ViewingResult<ViewingPage<CustomerViewingEventDto>>> HistoryAsync(Guid actor, Guid id, int page, int size, CancellationToken ct)
    {
        if (!await Accounts.ContainsAsync(actor, ct)) return new(CommandStatus.Forbidden);
        if (!await Scoped(actor).AnyAsync(v => v.Id == id, ct)) return new(CommandStatus.NotFound);
        var q = Events(actor, id); var total = await q.CountAsync(ct);
        var rows = await q.OrderBy(e => e.ViewingVersion).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success, new(rows.Select(e => e.ToCustomerDto()).ToArray(), total, page, size));
    }
    public async Task<ViewingResult<CustomerViewingEventDto>> EventAsync(Guid actor, Guid id, Guid eventId, CancellationToken ct)
    {
        if (!await Accounts.ContainsAsync(actor, ct)) return new(CommandStatus.Forbidden);
        var e = await Events(actor, id).SingleOrDefaultAsync(e => e.Id == eventId, ct);
        return new(e is null ? CommandStatus.NotFound : CommandStatus.Success, e?.ToCustomerDto());
    }
}
