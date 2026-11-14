using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Deals;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Deals;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController,PrivateCrmResponse,Authorize(Policy=AuthorizationPolicies.SalesManager)]
public sealed class DealsController(IDealService service):ControllerBase
{
    [HttpGet("api/crm/customers/{customerId:guid}/leads/{leadId:guid}/sale-units")]
    public async Task<IActionResult> PreparationUnits(Guid customerId,Guid leadId,CancellationToken ct,int page=1,int pageSize=20,string? q=null)
    {
        foreach(var name in new[]{"page","pageSize","q"})
            if(Request.Query.TryGetValue(name,out var values)&&(values.Count!=1||string.IsNullOrWhiteSpace(values[0])))
                return BadRequest(new ProblemDetails{Status=400,Title="Specify one nonblank value for each sale preparation filter."});
        return Map(await service.PreparationUnitsAsync(customerId,leadId,page,pageSize,q,ct));
    }
    [HttpGet("api/crm/customers/{customerId:guid}/leads/{leadId:guid}/sale-units/{unitId:guid}")]
    public async Task<IActionResult> PreparationUnit(Guid customerId,Guid leadId,Guid unitId,CancellationToken ct)=>
        Map(await service.PreparationUnitAsync(customerId,leadId,unitId,ct));
    [HttpPost("api/crm/customers/{customerId:guid}/deals")]
    public async Task<IActionResult> Adopt(Guid customerId,AdoptDealRequest request,[FromHeader(Name="Idempotency-Key")]string? key,CancellationToken ct)
    {
        var result=await service.AdoptAsync(customerId,new(request.UnitId!.Value,request.UnitVersion!.Value,request.LeadId!.Value,
            request.LeadVersion!.Value,request.FinalUnitPrice!.Value,request.CurrencyCode,request.SaleOccurredAtUtc!.Value,
            request.Source,request.Reference,request.SelectionReason,request.BookingId,request.BookingVersion),key??"",ct);
        return result.Status==CommandStatus.Success?CreatedAtAction(nameof(Get),new{customerId,id=result.Value!.Id},result.Value):Map(result);
    }
    [HttpGet("api/crm/customers/{customerId:guid}/deals/{id:guid}")]
    public async Task<IActionResult> Get(Guid customerId,Guid id,CancellationToken ct)=>Map(await service.GetAsync(customerId,id,ct));
    [HttpGet("api/crm/customers/{customerId:guid}/deals/{id:guid}/commission-snapshot")]
    public async Task<IActionResult> Snapshot(Guid customerId,Guid id,CancellationToken ct)=>Map(await service.SnapshotAsync(customerId,id,ct));
    [HttpGet("api/crm/deals")]
    public async Task<IActionResult> List(CancellationToken ct,int page=1,int pageSize=20,Guid? customerId=null,
        Guid? projectId=null,Guid? unitId=null,Guid? developerId=null,Guid? agreementId=null,string? currencyCode=null,
        DateOnly? saleLocalFrom=null,DateOnly? saleLocalThrough=null)
    {
        // MVC normalizes blank nullable query values to null; preserve omitted vs invalid input.
        foreach(var name in new[]{"page","pageSize","customerId","projectId","unitId","developerId","agreementId","currencyCode","saleLocalFrom","saleLocalThrough"})
            if(Request.Query.TryGetValue(name,out var values)&&(values.Count!=1||string.IsNullOrWhiteSpace(values[0])))
                return BadRequest(new ProblemDetails{Status=400,Title="Specify one nonblank value for each sale filter."});
        return Map(await service.ListAsync(new(page,pageSize,customerId,projectId,unitId,developerId,agreementId,currencyCode,saleLocalFrom,saleLocalThrough),ct));
    }
    private IActionResult Map<T>(DealResult<T> result)=>result.Status switch
    {
        CommandStatus.Success=>Ok(result.Value),CommandStatus.Forbidden=>Forbid(),
        CommandStatus.NotFound=>NotFound(new ProblemDetails{Status=404,Title="Sale resource was not found."}),
        CommandStatus.Invalid=>BadRequest(new ProblemDetails{Status=400,Title="Invalid documented external sale."}),
        CommandStatus.Conflict=>Conflict(new ProblemDetails{Status=409,Title="Current origin, Unit, booking, commission eligibility or idempotency key conflicts. Reload before adoption."}),
        _=>Problem(statusCode:503,title:"Sale adoption is unavailable.")
    };
}
