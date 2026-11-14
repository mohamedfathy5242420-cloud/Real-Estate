using BrokerHub.Application.DTOs.Deals;
using BrokerHub.Application.DTOs.Reports;

namespace BrokerHub.Application.Interfaces.Services;

public interface ISalesReportService
{
    Task<DealResult<CrmActivityReportDto>> ActivityAsync(ActivityReportFilterDto filter,CancellationToken ct);
    Task<DealResult<DealPage<CommissionCurrencyTotalDto>>> CommissionsAsync(CommissionReportFilterDto filter,CancellationToken ct);
}
