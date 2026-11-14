using BrokerHub.Application.DTOs.Leads;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;
public interface ILeadRepository
{
    Task<Lead?> LockOpenAsync(Guid customerId, Guid projectId, CancellationToken ct);
    Task<Lead?> LockByIdAsync(Guid customerId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<Lead>> SaleCandidatesAsync(Guid customerId,Guid projectId,CancellationToken ct);
    Task<MarketingAgreement?> LockOriginalAgreementAsync(Guid id,Guid developerId,CancellationToken ct);
    Task<LeadProjectOrigin?> LockProjectOriginAsync(Guid projectId, CancellationToken ct);
    Task<IReadOnlyList<LeadAgreementOrigin>> LockAgreementsAsync(Guid developerId, Guid projectId, DateOnly localDate, CancellationToken ct);
    void Add(Lead lead);
    void AddEvent(LeadEvent history);
}
