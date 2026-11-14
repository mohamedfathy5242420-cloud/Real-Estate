using BrokerHub.Application.DTOs.Customers;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface ICustomerLinkQueries
{
    Task<OwnCustomerLinkDto?> GetOwnAsync(Guid id, Guid accountId, CancellationToken ct);
    Task<CustomerLinkPage<OwnCustomerLinkDto>> GetOwnPageAsync(Guid accountId, int page, int size, CancellationToken ct);
    Task<CustomerLinkReviewDto?> GetReviewAsync(Guid id, CancellationToken ct);
    Task<CustomerLinkPage<CustomerLinkReviewDto>> GetPendingPageAsync(int page, int size, CancellationToken ct);
    Task<CustomerLinkPage<CustomerLinkCandidateDto>> GetCandidatesAsync(int page, int size, CancellationToken ct);
}
