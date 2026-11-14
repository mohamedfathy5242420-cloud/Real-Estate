using BrokerHub.Application.DTOs.MarketingAgreements;

namespace BrokerHub.Application.Interfaces.Services;

public interface IMarketingAgreementService
{
    Task<MarketingAgreementResult<MarketingAgreementDto>> CreateAsync(CreateMarketingAgreementDto request, CancellationToken cancellationToken);
    Task<MarketingAgreementResult<MarketingAgreementDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<MarketingAgreementResult<IReadOnlyList<MarketingAgreementDto>>> ListAsync(Guid? developerId, int skip, int take, CancellationToken cancellationToken);
    Task<MarketingAgreementResult<MarketingAgreementDto>> UpdateAsync(Guid id, UpdateMarketingAgreementDto request, CancellationToken cancellationToken);
    Task<MarketingAgreementResult<MarketingAgreementDto>> ActivateAsync(Guid id, long version, CancellationToken cancellationToken);
}
