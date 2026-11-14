using BrokerHub.Api.Authorization;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Deals;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController,PrivateCrmResponse]
public sealed class SalesReportsController(ISalesReportService service):ControllerBase
{
    [HttpGet("api/crm/reports/activity"),Authorize(Policy=AuthorizationPolicies.SalesStaff)]
    public async Task<IActionResult> Activity(CancellationToken ct,DateOnly? fromUtcDate=null,DateOnly? throughUtcDate=null)=>
        BadQuery("fromUtcDate","throughUtcDate")?BadRequest(new ProblemDetails{Status=400,Title="Specify one nonblank value for each report filter."}):
        Map(await service.ActivityAsync(new(fromUtcDate,throughUtcDate),ct));
    [HttpGet("api/crm/reports/commissions"),Authorize(Policy=AuthorizationPolicies.SalesManager)]
    public async Task<IActionResult> Commissions(CancellationToken ct,int page=1,int pageSize=20,Guid? customerId=null,
        Guid? projectId=null,Guid? developerId=null,string? currencyCode=null,DateOnly? saleLocalFrom=null,DateOnly? saleLocalThrough=null)=>
        BadQuery("page","pageSize","customerId","projectId","developerId","currencyCode","saleLocalFrom","saleLocalThrough")?
        BadRequest(new ProblemDetails{Status=400,Title="Specify one nonblank value for each report filter."}):
        Map(await service.CommissionsAsync(new(page,pageSize,customerId,projectId,developerId,currencyCode,saleLocalFrom,saleLocalThrough),ct));
    private bool BadQuery(params string[] names)=>names.Any(name=>Request.Query.TryGetValue(name,out var values)&&(values.Count!=1||string.IsNullOrWhiteSpace(values[0])));
    private IActionResult Map<T>(DealResult<T> result)=>result.Status switch
    {
        CommandStatus.Success=>Ok(result.Value),CommandStatus.Forbidden=>Forbid(),
        CommandStatus.Invalid=>BadRequest(new ProblemDetails{Status=400,Title="Invalid report scope, page or dates."}),
        CommandStatus.Conflict=>Conflict(new ProblemDetails{Status=409,Title="Recorded financial ledger requires reconciliation before reporting."}),
        _=>Problem(statusCode:503,title:"The report is temporarily unavailable.")
    };
}
