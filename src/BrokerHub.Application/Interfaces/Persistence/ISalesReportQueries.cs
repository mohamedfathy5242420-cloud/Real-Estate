using BrokerHub.Application.DTOs.Deals;
using BrokerHub.Application.DTOs.Reports;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface ISalesReportQueries
{
    Task<DealResult<CrmActivityReportDto>> ActivityAsync(Guid actor,ActivityReportFilterDto filter,DateTime observedUtc,CancellationToken ct);
    Task<DealResult<DealPage<CommissionCurrencyTotalDto>>> CommissionsAsync(Guid actor,CommissionReportFilterDto filter,CancellationToken ct);
}
