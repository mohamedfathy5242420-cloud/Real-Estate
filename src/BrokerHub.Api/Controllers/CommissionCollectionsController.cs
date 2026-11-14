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
public sealed class CommissionCollectionsController(ICommissionCollectionService service):ControllerBase
{
    [HttpPost("api/crm/customers/{customerId:guid}/deals/{dealId:guid}/collections")]
    public async Task<IActionResult> Record(Guid customerId,Guid dealId,CommissionCollectionRequest request,[FromHeader(Name="Idempotency-Key")]string? key,CancellationToken ct)
    {
        var result=await service.RecordAsync(customerId,dealId,new(request.Version!.Value,request.Amount!.Value,request.CurrencyCode,
            request.CollectedAtUtc!.Value,request.Source,request.Reference,request.Notes),key??"",ct);
        return result.Status==CommandStatus.Success?CreatedAtAction(nameof(Get),new{customerId,dealId,id=result.Value!.Id},result.Value):Map(result);
    }
    [HttpGet("api/crm/customers/{customerId:guid}/deals/{dealId:guid}/collections/{id:guid}")]
    public async Task<IActionResult> Get(Guid customerId,Guid dealId,Guid id,CancellationToken ct)=>Map(await service.GetAsync(customerId,dealId,id,ct));
    [HttpGet("api/crm/customers/{customerId:guid}/deals/{dealId:guid}/collections")]
    public async Task<IActionResult> List(Guid customerId,Guid dealId,CancellationToken ct,int page=1,int pageSize=20)
    {
        foreach(var name in new[]{"page","pageSize"})
            if(Request.Query.TryGetValue(name,out var values)&&(values.Count!=1||string.IsNullOrWhiteSpace(values[0])))
                return BadRequest(new ProblemDetails{Status=400,Title="Specify one nonblank value for each collection page parameter."});
        return Map(await service.ListAsync(customerId,dealId,page,pageSize,ct));
    }
    [HttpGet("api/crm/customers/{customerId:guid}/deals/{dealId:guid}/collection-summary")]
    public async Task<IActionResult> Summary(Guid customerId,Guid dealId,CancellationToken ct)=>Map(await service.SummaryAsync(customerId,dealId,ct));
    private IActionResult Map<T>(DealResult<T> result)=>result.Status switch
    {
        CommandStatus.Success=>Ok(result.Value),CommandStatus.Forbidden=>Forbid(),
        CommandStatus.NotFound=>NotFound(new ProblemDetails{Status=404,Title="Commission collection resource was not found."}),
        CommandStatus.Invalid=>BadRequest(new ProblemDetails{Status=400,Title="Invalid actual external commission collection."}),
        CommandStatus.Conflict=>Conflict(new ProblemDetails{Status=409,Title="Current entitlement, recorded balance, version, audit clock or idempotency key conflicts."}),
        _=>Problem(statusCode:503,title:"Commission collection is temporarily unavailable.")
    };
}
