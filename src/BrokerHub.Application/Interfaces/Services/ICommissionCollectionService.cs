using BrokerHub.Application.DTOs.Deals;

namespace BrokerHub.Application.Interfaces.Services;

public interface ICommissionCollectionService
{
    Task<DealResult<DealReply>> RecordAsync(Guid customerId,Guid dealId,RecordCommissionCollectionDto request,string key,CancellationToken ct);
    Task<DealResult<DealPage<ManagerCommissionCollectionDto>>> ListAsync(Guid customerId,Guid dealId,int page,int size,CancellationToken ct);
    Task<DealResult<ManagerCommissionCollectionDto>> GetAsync(Guid customerId,Guid dealId,Guid id,CancellationToken ct);
    Task<DealResult<CommissionCollectionSummaryDto>> SummaryAsync(Guid customerId,Guid dealId,CancellationToken ct);
}
