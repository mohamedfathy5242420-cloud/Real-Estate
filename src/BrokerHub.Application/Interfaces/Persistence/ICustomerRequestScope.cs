using BrokerHub.Application.DTOs.Requests;

namespace BrokerHub.Application.Interfaces.Persistence;
public interface ICustomerRequestScope
{
    Task<CustomerRequestLock> LockOwnedAsync(Guid actor, CancellationToken ct);
}
