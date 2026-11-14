using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Deals;
using BrokerHub.Application.DTOs.Reports;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;

namespace BrokerHub.Application.Services;

public sealed class SalesReportService(ISalesReportQueries queries,ICurrentUser user,TimeProvider clock):ISalesReportService
{
    private Guid? Actor(bool manager)=>user.IsAuthenticated&&user.AccountId is { } actor&&actor!=Guid.Empty&&
        (user.IsInRole(AppRoles.SalesManager)||!manager&&user.IsInRole(AppRoles.SalesEmployee))?actor:null;
    private static bool Dates(DateOnly? from,DateOnly? through)=>from!=default(DateOnly)&&through!=default(DateOnly)&&!(from>through);
    public Task<DealResult<CrmActivityReportDto>> ActivityAsync(ActivityReportFilterDto filter,CancellationToken ct)=>
        Actor(false) is not { } actor?Task.FromResult(new DealResult<CrmActivityReportDto>(CommandStatus.Forbidden)):
        !Dates(filter.FromUtcDate,filter.ThroughUtcDate)?Task.FromResult(new DealResult<CrmActivityReportDto>(CommandStatus.Invalid)):
        queries.ActivityAsync(actor,filter,clock.GetUtcNow().UtcDateTime,ct);
    public Task<DealResult<DealPage<CommissionCurrencyTotalDto>>> CommissionsAsync(CommissionReportFilterDto filter,CancellationToken ct)
    {
        if(Actor(true) is not { } actor)return Task.FromResult(new DealResult<DealPage<CommissionCurrencyTotalDto>>(CommandStatus.Forbidden));
        var raw=filter.CurrencyCode?.Trim();
        if(filter.Page is <=0 or >1000000||filter.PageSize is <=0 or >100||filter.CustomerId==Guid.Empty||
            filter.ProjectId==Guid.Empty||filter.DeveloperId==Guid.Empty||!Dates(filter.SaleLocalFrom,filter.SaleLocalThrough)||
            filter.CurrencyCode is not null&&(filter.CurrencyCode.Length>16||raw!.Length!=3||raw.Any(c=>c is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z'))))
            return Task.FromResult(new DealResult<DealPage<CommissionCurrencyTotalDto>>(CommandStatus.Invalid));
        return queries.CommissionsAsync(actor,filter with{CurrencyCode=raw?.ToUpperInvariant()},ct);
    }
}
