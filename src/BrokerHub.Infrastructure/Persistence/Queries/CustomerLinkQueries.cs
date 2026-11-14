using BrokerHub.Application.DTOs.Customers;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
using BrokerHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class CustomerLinkQueries(ApplicationDbContext db) : ICustomerLinkQueries
{
    public async Task<OwnCustomerLinkDto?> GetOwnAsync(Guid id, Guid accountId, CancellationToken ct) =>
        (await db.CustomerLinkRequests.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id && r.AccountId == accountId, ct))?.ToOwnDto();
    public async Task<CustomerLinkPage<OwnCustomerLinkDto>> GetOwnPageAsync(Guid accountId, int page, int size, CancellationToken ct)
    {
        var query = db.CustomerLinkRequests.AsNoTracking().Where(r => r.AccountId == accountId);
        var count = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(r => r.CreatedAtUtc).ThenBy(r => r.Id).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(rows.Select(r => r.ToOwnDto()).ToArray(), page, size, count);
    }
    public async Task<CustomerLinkReviewDto?> GetReviewAsync(Guid id, CancellationToken ct) =>
        (await db.CustomerLinkRequests.AsNoTracking().Include(r => r.History).SingleOrDefaultAsync(r => r.Id == id, ct))?.ToReviewDto();
    public async Task<CustomerLinkPage<CustomerLinkReviewDto>> GetPendingPageAsync(int page, int size, CancellationToken ct)
    {
        var query = db.CustomerLinkRequests.AsNoTracking().Where(r => r.Status == CustomerLinkRequestStatus.Pending);
        var count = await query.CountAsync(ct);
        var rows = await query.Include(r => r.History).OrderBy(r => r.CreatedAtUtc).ThenBy(r => r.Id).Skip((page - 1) * size).Take(size).ToArrayAsync(ct);
        return new(rows.Select(r => r.ToReviewDto()).ToArray(), page, size, count);
    }
    public async Task<CustomerLinkPage<CustomerLinkCandidateDto>> GetCandidatesAsync(int page, int size, CancellationToken ct)
    {
        var query = db.Customers.AsNoTracking().Where(c => c.AccountId == null);
        var count = await query.CountAsync(ct);
        var rows = await query.OrderBy(c => c.FullName).ThenBy(c => c.Id).Skip((page - 1) * size).Take(size)
            .Select(c => new CustomerLinkCandidateDto(c.Id, c.FullName, c.Phone, c.Email, c.Version)).ToArrayAsync(ct);
        return new(rows, page, size, count);
    }
}
