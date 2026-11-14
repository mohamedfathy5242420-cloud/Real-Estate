using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface ICustomerRepository
{
    Task<Customer?> GetOwnedTrackedAsync(Guid customerId, Guid accountId, CancellationToken ct);
    Task AddAsync(Customer customer, CancellationToken ct);
}
