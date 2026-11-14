using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IMarketingAgreementRepository
{
    Task<MarketingAgreement?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> NumberExistsAsync(Guid developerId, string number, Guid? exceptId, CancellationToken cancellationToken);
    Task AddAsync(MarketingAgreement agreement, CancellationToken cancellationToken);
    Task AddRevisionAsync(MarketingAgreementRevision revision, CancellationToken cancellationToken);
    /// <summary>Executes one fresh activation command under a persisted active-Developer lock through commit.
    /// The callback owns the single UnitOfWork save; retries reload the aggregate, not replay mutated state.</summary>
    Task<T> ExecuteActivationAsync<T>(Guid developerId, Func<CancellationToken, Task<T>> command, CancellationToken cancellationToken);
    /// <summary>Checks symbolic inclusive-date coverage; call only inside the activation transaction.</summary>
    Task<bool> ActiveOverlapExistsAsync(MarketingAgreement agreement, CancellationToken cancellationToken);
}
