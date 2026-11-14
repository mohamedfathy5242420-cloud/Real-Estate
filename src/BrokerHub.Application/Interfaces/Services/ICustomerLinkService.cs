using BrokerHub.Application.DTOs.Customers;

namespace BrokerHub.Application.Interfaces.Services;

public interface ICustomerLinkService
{
    Task<CustomerLinkResult<OwnCustomerLinkDto>> SubmitAsync(string claim, CancellationToken ct);
    Task<CustomerLinkResult<OwnCustomerLinkDto>> GetOwnAsync(Guid id, CancellationToken ct);
    Task<CustomerLinkResult<CustomerLinkPage<OwnCustomerLinkDto>>> GetOwnPageAsync(int page, int size, CancellationToken ct);
    Task<CustomerLinkResult<CustomerLinkReviewDto>> GetReviewAsync(Guid id, CancellationToken ct);
    Task<CustomerLinkResult<CustomerLinkPage<CustomerLinkReviewDto>>> GetPendingPageAsync(int page, int size, CancellationToken ct);
    Task<CustomerLinkResult<CustomerLinkPage<CustomerLinkCandidateDto>>> GetCandidatesAsync(int page, int size, CancellationToken ct);
    Task<CustomerLinkResult<CustomerLinkReviewDto>> DecideAsync(Guid id, bool approve, ReviewCustomerLinkDto command, CancellationToken ct);
}
