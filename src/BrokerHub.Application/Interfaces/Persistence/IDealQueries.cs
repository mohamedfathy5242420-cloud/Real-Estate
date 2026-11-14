using BrokerHub.Application.DTOs.Deals;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IDealQueries
{
    Task<DealResult<DealPage<DealPreparationUnitDto>>> PreparationUnitsAsync(Guid actor,Guid customerId,Guid leadId,int page,int size,string? q,CancellationToken ct);
    Task<DealResult<DealPreparationUnitDto>> PreparationUnitAsync(Guid actor,Guid customerId,Guid leadId,Guid unitId,CancellationToken ct);
    Task<DealResult<ManagerDealDto>> GetAsync(Guid actor,Guid customerId,Guid id,CancellationToken ct);
    Task<DealResult<CommissionSnapshotDto>> SnapshotAsync(Guid actor,Guid customerId,Guid id,CancellationToken ct);
    Task<DealResult<DealPage<ManagerDealSummaryDto>>> ListAsync(Guid actor,DealFilterDto filter,CancellationToken ct);
    Task<DealResult<DealPage<ManagerCommissionCollectionDto>>> CollectionsAsync(Guid actor,Guid customerId,Guid dealId,int page,int size,CancellationToken ct);
    Task<DealResult<ManagerCommissionCollectionDto>> CollectionAsync(Guid actor,Guid customerId,Guid dealId,Guid id,CancellationToken ct);
    Task<DealResult<CommissionCollectionSummaryDto>> CollectionSummaryAsync(Guid actor,Guid customerId,Guid dealId,CancellationToken ct);
}
