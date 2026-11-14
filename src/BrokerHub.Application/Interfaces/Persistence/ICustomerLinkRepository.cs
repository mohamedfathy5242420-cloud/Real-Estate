using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface ICustomerLinkRepository
{
    Task<CustomerLinkRequest?> GetTrackedAsync(Guid id, CancellationToken ct);
    Task<Customer?> GetCustomerTrackedAsync(Guid id, CancellationToken ct);
    Task<bool> HasProfileAsync(Guid accountId, CancellationToken ct);
    void Add(CustomerLinkRequest request);
    // Protect current Identity eligibility through commit and save all tracked link/history effects atomically.
    Task CommitAsync(CustomerLinkRequest request, Customer? target, CancellationToken ct);
}
