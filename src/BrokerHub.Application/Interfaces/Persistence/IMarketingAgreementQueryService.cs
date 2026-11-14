using BrokerHub.Application.DTOs.MarketingAgreements;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IMarketingAgreementQueryService
{
    Task<MarketingAgreementDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MarketingAgreementDto>> ListAsync(Guid? developerId, int skip, int take, CancellationToken cancellationToken);
}
