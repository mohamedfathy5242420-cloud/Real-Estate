using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace BrokerHub.Infrastructure.Persistence.Queries;
internal sealed class BookingQueries(ApplicationDbContext db, TimeProvider clock) : IBookingQueries
{
    private IQueryable<Guid> AccountsIn(string role) => from ar in db.UserRoles join r in db.Roles on ar.RoleId equals r.Id where r.NormalizedName == role select ar.UserId;
    private Task<bool> Allowed(Guid actor, bool staff, CancellationToken ct)
    {
        var owners = AccountsIn("CUSTOMER"); var employees = AccountsIn("SALESEMPLOYEE"); var managers = AccountsIn("SALESMANAGER");
        return db.Users.AnyAsync(u => u.Id == actor && u.IsActive && u.EmailConfirmed &&
            (staff ? employees.Contains(actor) || managers.Contains(actor) : owners.Contains(actor)),ct);
    }
    private IQueryable<Customer> Customers(Guid actor, bool staff)
    {
        var owners = AccountsIn("CUSTOMER"); var employees = AccountsIn("SALESEMPLOYEE"); var managers = AccountsIn("SALESMANAGER");
        return db.Customers.AsNoTracking().Where(c => db.Users.Any(u => u.Id == actor && u.IsActive && u.EmailConfirmed) &&
            (staff ? managers.Contains(actor) || (employees.Contains(actor) && db.Employees.Any(e => e.Id == c.AssignedEmployeeId && e.AccountId == actor))
                : owners.Contains(actor) && c.AccountId == actor));
    }
    private IQueryable<Booking> Bookings(Guid actor, bool staff) => from b in db.Bookings.AsNoTracking() join c in Customers(actor,staff) on b.CustomerId equals c.Id select b;
    private IQueryable<BookingEvent> Events(Guid actor, bool staff, Guid id, Guid? customer = null) => from h in db.BookingEvents.AsNoTracking()
        join b in Bookings(actor,staff) on new { h.BookingId, h.CustomerId } equals new { BookingId = b.Id, b.CustomerId }
        where b.Id == id && (customer == null || b.CustomerId == customer) select h;
    public async Task<BookingResult<BookingPage<CustomerBookingDto>>> MineAsync(Guid actor, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor,false,ct)) return new(CommandStatus.Forbidden);
        if (!await Customers(actor,false).AnyAsync(ct)) return new(CommandStatus.NotFound);
        var observed = clock.GetUtcNow().UtcDateTime; var q = Bookings(actor,false); var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(b => b.RequestedAtUtc).ThenBy(b => b.Id).Skip((page-1)*size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success,new(rows.Select(b => b.ToCustomerDto(observed)).ToArray(),total,page,size));
    }
    public async Task<BookingResult<CustomerBookingDto>> OwnAsync(Guid actor, Guid id, CancellationToken ct)
    {
        if (!await Allowed(actor,false,ct)) return new(CommandStatus.Forbidden);
        var observed = clock.GetUtcNow().UtcDateTime; var b = await Bookings(actor,false).SingleOrDefaultAsync(b => b.Id == id,ct);
        return new(b is null ? CommandStatus.NotFound : CommandStatus.Success,b?.ToCustomerDto(observed));
    }
    public async Task<BookingResult<BookingPage<CustomerBookingEventDto>>> OwnHistoryAsync(Guid actor, Guid id, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor,false,ct)) return new(CommandStatus.Forbidden);
        if (!await Bookings(actor,false).AnyAsync(b => b.Id == id,ct)) return new(CommandStatus.NotFound);
        var q = Events(actor,false,id); var total = await q.CountAsync(ct);
        var rows = await q.OrderBy(h => h.BookingVersion).Skip((page-1)*size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success,new(rows.Select(h => h.ToCustomerDto()).ToArray(),total,page,size));
    }
    public async Task<BookingResult<CustomerBookingEventDto>> OwnEventAsync(Guid actor, Guid id, Guid eventId, CancellationToken ct)
    {
        if (!await Allowed(actor,false,ct)) return new(CommandStatus.Forbidden);
        var h = await Events(actor,false,id).SingleOrDefaultAsync(h => h.Id == eventId,ct);
        return new(h is null ? CommandStatus.NotFound : CommandStatus.Success,h?.ToCustomerDto());
    }
    public async Task<BookingResult<BookingPage<StaffBookingDto>>> StaffListAsync(Guid actor, Guid? customerId, string? status, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor,true,ct)) return new(CommandStatus.Forbidden);
        if (customerId is { } customer && !await Customers(actor,true).AnyAsync(c => c.Id == customer,ct)) return new(CommandStatus.NotFound);
        var observed = clock.GetUtcNow().UtcDateTime; var q = Bookings(actor,true).Where(b => customerId == null || b.CustomerId == customerId);
        if (status == Booking.PendingReview) q = q.Where(b => b.Status == Booking.PendingReview && b.ResponseDueAtUtc > observed);
        else if (status == Booking.Rejected) q = q.Where(b => b.Status == Booking.Rejected || b.Status == Booking.PendingReview && b.ResponseDueAtUtc <= observed);
        else if (status == Booking.Confirmed) q = q.Where(b => b.Status == Booking.Confirmed && b.ReservationExpiresAtUtc > observed);
        else if (status == Booking.Expired) q = q.Where(b => b.Status == Booking.Expired || b.Status == Booking.Confirmed && b.ReservationExpiresAtUtc <= observed);
        else if (status == Booking.Cancelled) q = q.Where(b => b.Status == Booking.Cancelled);
        else if (status == Booking.Completed) q = q.Where(b => b.Status == Booking.Completed);
        var total = await q.CountAsync(ct); var rows = await q.OrderByDescending(b => b.RequestedAtUtc).ThenBy(b => b.Id).Skip((page-1)*size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success,new(rows.Select(b => b.ToStaffDto(observed)).ToArray(),total,page,size));
    }
    public async Task<BookingResult<StaffBookingDto>> StaffAsync(Guid actor, Guid customerId, Guid id, CancellationToken ct)
    {
        if (!await Allowed(actor,true,ct)) return new(CommandStatus.Forbidden);
        var observed = clock.GetUtcNow().UtcDateTime; var b = await Bookings(actor,true).SingleOrDefaultAsync(b => b.Id == id && b.CustomerId == customerId,ct);
        return new(b is null ? CommandStatus.NotFound : CommandStatus.Success,b?.ToStaffDto(observed));
    }
    public async Task<BookingResult<BookingPage<StaffBookingEventDto>>> StaffHistoryAsync(Guid actor, Guid customerId, Guid id, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor,true,ct)) return new(CommandStatus.Forbidden);
        if (!await Bookings(actor,true).AnyAsync(b => b.Id == id && b.CustomerId == customerId,ct)) return new(CommandStatus.NotFound);
        var q = Events(actor,true,id,customerId); var total = await q.CountAsync(ct);
        var rows = await q.OrderBy(h => h.BookingVersion).Skip((page-1)*size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success,new(rows.Select(h => h.ToStaffDto()).ToArray(),total,page,size));
    }
    public async Task<BookingResult<StaffBookingEventDto>> StaffEventAsync(Guid actor, Guid customerId, Guid id, Guid eventId, CancellationToken ct)
    {
        if (!await Allowed(actor,true,ct)) return new(CommandStatus.Forbidden);
        var h = await Events(actor,true,id,customerId).SingleOrDefaultAsync(h => h.Id == eventId,ct);
        return new(h is null ? CommandStatus.NotFound : CommandStatus.Success,h?.ToStaffDto());
    }
    private IQueryable<BookingDeveloperResponse> Responses(Guid actor, Guid customerId, Guid id) => from r in db.BookingDeveloperResponses.AsNoTracking()
        join b in Bookings(actor,true) on new { r.BookingId,r.CustomerId } equals new { BookingId=b.Id,b.CustomerId }
        where b.Id == id && b.CustomerId == customerId select r;
    public async Task<BookingResult<BookingPage<StaffBookingResponseDto>>> ResponsesAsync(Guid actor, Guid customerId, Guid id, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor,true,ct)) return new(CommandStatus.Forbidden);
        if (!await Bookings(actor,true).AnyAsync(b => b.Id == id && b.CustomerId == customerId,ct)) return new(CommandStatus.NotFound);
        var q = Responses(actor,customerId,id); var total = await q.CountAsync(ct);
        var rows = await q.OrderBy(r => r.RecordedAtUtc).ThenBy(r => r.Id).Skip((page-1)*size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success,new(rows.Select(r => r.ToStaffDto()).ToArray(),total,page,size));
    }
    public async Task<BookingResult<StaffBookingResponseDto>> ResponseAsync(Guid actor, Guid customerId, Guid id, Guid responseId, CancellationToken ct)
    {
        if (!await Allowed(actor,true,ct)) return new(CommandStatus.Forbidden);
        var r = await Responses(actor,customerId,id).SingleOrDefaultAsync(r => r.Id == responseId,ct);
        return new(r is null ? CommandStatus.NotFound : CommandStatus.Success,r?.ToStaffDto());
    }
    private IQueryable<BookingExtension> Extensions(Guid actor, Guid customerId, Guid id) => from e in db.BookingExtensions.AsNoTracking()
        join b in Bookings(actor,true) on new { e.BookingId,e.CustomerId } equals new { BookingId=b.Id,b.CustomerId }
        where b.Id == id && b.CustomerId == customerId select e;
    public async Task<BookingResult<BookingPage<StaffBookingExtensionDto>>> ExtensionsAsync(Guid actor, Guid customerId, Guid id, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor,true,ct)) return new(CommandStatus.Forbidden);
        if (!await Bookings(actor,true).AnyAsync(b => b.Id == id && b.CustomerId == customerId,ct)) return new(CommandStatus.NotFound);
        var q = Extensions(actor,customerId,id);var total = await q.CountAsync(ct);
        var rows = await q.OrderBy(e => e.RecordedAtUtc).ThenBy(e => e.Id).Skip((page-1)*size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success,new(rows.Select(e => e.ToStaffDto()).ToArray(),total,page,size));
    }
    public async Task<BookingResult<StaffBookingExtensionDto>> ExtensionAsync(Guid actor, Guid customerId, Guid id, Guid extensionId, CancellationToken ct)
    {
        if (!await Allowed(actor,true,ct)) return new(CommandStatus.Forbidden);
        var e = await Extensions(actor,customerId,id).SingleOrDefaultAsync(e => e.Id == extensionId,ct);
        return new(e is null ? CommandStatus.NotFound : CommandStatus.Success,e?.ToStaffDto());
    }
    private IQueryable<BookingDeposit> Deposits(Guid actor, bool staff, Guid id, Guid? customerId = null) => from d in db.BookingDeposits.AsNoTracking()
        join b in Bookings(actor,staff) on new { d.BookingId,d.CustomerId,d.UnitId } equals new { BookingId=b.Id,b.CustomerId,b.UnitId }
        where b.Id == id && (customerId == null || b.CustomerId == customerId) select d;
    private sealed record DepositAmountRow(BookingDeposit Deposit, decimal Returned);
    private IQueryable<DepositAmountRow> DepositAmounts(IQueryable<BookingDeposit> source) => source.Select(d =>
        new DepositAmountRow(d,db.BookingRefunds.Where(r => r.DepositId == d.Id).Sum(r => (decimal?)r.Amount) ?? 0m));
    public async Task<BookingResult<BookingPage<StaffBookingDepositDto>>> DepositsAsync(Guid actor, Guid customerId, Guid id, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor,true,ct)) return new(CommandStatus.Forbidden);
        if (!await Bookings(actor,true).AnyAsync(b => b.Id == id && b.CustomerId == customerId,ct)) return new(CommandStatus.NotFound);
        var q = Deposits(actor,true,id,customerId); var total = await q.CountAsync(ct);
        var rows = await DepositAmounts(q.OrderBy(d => d.RecordedAtUtc).ThenBy(d => d.Id).Skip((page-1)*size).Take(size)).ToArrayAsync(ct);
        return new(CommandStatus.Success,new(rows.Select(d => d.Deposit.ToStaffDto(d.Returned)).ToArray(),total,page,size));
    }
    public async Task<BookingResult<StaffBookingDepositDto>> DepositAsync(Guid actor, Guid customerId, Guid id, Guid depositId, CancellationToken ct)
    {
        if (!await Allowed(actor,true,ct)) return new(CommandStatus.Forbidden);
        var d = await DepositAmounts(Deposits(actor,true,id,customerId).Where(d => d.Id == depositId)).SingleOrDefaultAsync(ct);
        return d is null ? new(CommandStatus.NotFound) : new(CommandStatus.Success,d.Deposit.ToStaffDto(d.Returned));
    }
    public async Task<BookingResult<BookingPage<CustomerBookingDepositDto>>> OwnDepositsAsync(Guid actor, Guid id, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor,false,ct)) return new(CommandStatus.Forbidden);
        if (!await Bookings(actor,false).AnyAsync(b => b.Id == id,ct)) return new(CommandStatus.NotFound);
        var q = Deposits(actor,false,id); var total = await q.CountAsync(ct);
        var rows = await DepositAmounts(q.OrderBy(d => d.RecordedAtUtc).ThenBy(d => d.Id).Skip((page-1)*size).Take(size)).ToArrayAsync(ct);
        return new(CommandStatus.Success,new(rows.Select(d => d.Deposit.ToCustomerDto(d.Returned)).ToArray(),total,page,size));
    }
    public async Task<BookingResult<CustomerBookingDepositDto>> OwnDepositAsync(Guid actor, Guid id, Guid depositId, CancellationToken ct)
    {
        if (!await Allowed(actor,false,ct)) return new(CommandStatus.Forbidden);
        var d = await DepositAmounts(Deposits(actor,false,id).Where(d => d.Id == depositId)).SingleOrDefaultAsync(ct);
        return d is null ? new(CommandStatus.NotFound) : new(CommandStatus.Success,d.Deposit.ToCustomerDto(d.Returned));
    }
    private IQueryable<BookingRefund> Refunds(Guid actor, bool staff, Guid id, Guid depositId, Guid? customerId = null) =>
        from r in db.BookingRefunds.AsNoTracking() join d in Deposits(actor,staff,id,customerId)
        on new { r.DepositId,r.BookingId,r.CustomerId,r.UnitId } equals new { DepositId=d.Id,d.BookingId,d.CustomerId,d.UnitId }
        where d.Id == depositId select r;
    public async Task<BookingResult<BookingPage<StaffBookingRefundDto>>> RefundsAsync(Guid actor, Guid customerId, Guid id, Guid depositId, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor,true,ct)) return new(CommandStatus.Forbidden);
        if (!await Deposits(actor,true,id,customerId).AnyAsync(d => d.Id == depositId,ct)) return new(CommandStatus.NotFound);
        var q=Refunds(actor,true,id,depositId,customerId);var total=await q.CountAsync(ct);
        var rows=await q.OrderBy(r => r.RecordedAtUtc).ThenBy(r => r.Id).Skip((page-1)*size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success,new(rows.Select(r => r.ToStaffDto()).ToArray(),total,page,size));
    }
    public async Task<BookingResult<StaffBookingRefundDto>> RefundAsync(Guid actor, Guid customerId, Guid id, Guid depositId, Guid refundId, CancellationToken ct)
    {
        if (!await Allowed(actor,true,ct)) return new(CommandStatus.Forbidden);
        var r=await Refunds(actor,true,id,depositId,customerId).SingleOrDefaultAsync(r => r.Id == refundId,ct);
        return new(r is null?CommandStatus.NotFound:CommandStatus.Success,r?.ToStaffDto());
    }
    public async Task<BookingResult<BookingPage<CustomerBookingRefundDto>>> OwnRefundsAsync(Guid actor, Guid id, Guid depositId, int page, int size, CancellationToken ct)
    {
        if (!await Allowed(actor,false,ct)) return new(CommandStatus.Forbidden);
        if (!await Deposits(actor,false,id).AnyAsync(d => d.Id == depositId,ct)) return new(CommandStatus.NotFound);
        var q=Refunds(actor,false,id,depositId);var total=await q.CountAsync(ct);
        var rows=await q.OrderBy(r => r.RecordedAtUtc).ThenBy(r => r.Id).Skip((page-1)*size).Take(size).ToArrayAsync(ct);
        return new(CommandStatus.Success,new(rows.Select(r => r.ToCustomerDto()).ToArray(),total,page,size));
    }
    public async Task<BookingResult<CustomerBookingRefundDto>> OwnRefundAsync(Guid actor, Guid id, Guid depositId, Guid refundId, CancellationToken ct)
    {
        if (!await Allowed(actor,false,ct)) return new(CommandStatus.Forbidden);
        var r=await Refunds(actor,false,id,depositId).SingleOrDefaultAsync(r => r.Id == refundId,ct);
        return new(r is null?CommandStatus.NotFound:CommandStatus.Success,r?.ToCustomerDto());
    }
}
