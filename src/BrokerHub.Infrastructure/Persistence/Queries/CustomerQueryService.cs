using BrokerHub.Application.DTOs.Customers;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class CustomerQueryService(ApplicationDbContext db) : ICustomerQueryService
{
    public async Task<CustomerProfileDto?> GetOwnedAsync(Guid accountId, Guid? customerId, CancellationToken ct)
    {
        var query = db.Customers.AsNoTracking().Where(c => c.AccountId == accountId);
        if (customerId.HasValue) query = query.Where(c => c.Id == customerId.Value);
        var customer = await query.SingleOrDefaultAsync(ct);
        return customer?.ToProfileDto();
    }
}
