using BrokerHub.Application.DTOs.Customers;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface ICustomerQueryService
{
    Task<CustomerProfileDto?> GetOwnedAsync(Guid accountId, Guid? customerId, CancellationToken ct);
}
