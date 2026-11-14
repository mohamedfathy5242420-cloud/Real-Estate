using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface ICrmCustomerRepository
{
    // Called only inside ICommandExecutor. Protect current role and root scope, never old authorship.
    Task<CustomerSalesLock> LockForSalesAsync(Guid customerId, Guid actor, bool managerOnly, CancellationToken ct);
    Task<bool> LockEligibleAssigneeAsync(Guid employeeId, CancellationToken ct);
    void AddAssignment(CustomerAssignment assignment);
}
