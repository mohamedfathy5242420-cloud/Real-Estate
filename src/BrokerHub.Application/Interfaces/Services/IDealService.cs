using BrokerHub.Application.DTOs.Deals;

namespace BrokerHub.Application.Interfaces.Services;

public interface IDealService
{
    Task<DealResult<DealPage<DealPreparationUnitDto>>> PreparationUnitsAsync(Guid customerId,Guid leadId,int page,int size,string? q,CancellationToken ct);
    Task<DealResult<DealPreparationUnitDto>> PreparationUnitAsync(Guid customerId,Guid leadId,Guid unitId,CancellationToken ct);
    Task<DealResult<DealReply>> AdoptAsync(Guid customerId,AdoptDealDto request,string key,CancellationToken ct);
    Task<DealResult<ManagerDealDto>> GetAsync(Guid customerId,Guid id,CancellationToken ct);
    Task<DealResult<CommissionSnapshotDto>> SnapshotAsync(Guid customerId,Guid id,CancellationToken ct);
    Task<DealResult<DealPage<ManagerDealSummaryDto>>> ListAsync(DealFilterDto filter,CancellationToken ct);
}
