using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository(ApplicationDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetOwnedTrackedAsync(Guid customerId, Guid accountId, CancellationToken ct) =>
        db.Customers.SingleOrDefaultAsync(c => c.Id == customerId && c.AccountId == accountId, ct);
    public async Task AddAsync(Customer customer, CancellationToken ct) => await db.Customers.AddAsync(customer, ct);
}
