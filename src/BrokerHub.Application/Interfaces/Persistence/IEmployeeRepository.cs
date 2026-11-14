using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IEmployeeRepository
{
    Task<Employee?> GetTrackedAsync(Guid id, CancellationToken ct);
    void Add(Employee employee);
    // Atomic root/revision save while live actor and active target staff eligibility are protected.
    Task CommitAsync(Employee employee, Guid actor, CancellationToken ct);
}
