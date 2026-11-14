using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Bookings;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BrokerHub.Api.Controllers;
[ApiController,PrivateCrmResponse,Authorize(Policy=AuthorizationPolicies.SalesStaff)]
public sealed class BookingRefundsController(IBookingRefundService service) : ControllerBase
{
    [HttpPost("api/crm/customers/{customerId:guid}/bookings/{id:guid}/deposits/{depositId:guid}/refunds")]
    public async Task<IActionResult> Record(Guid customerId,Guid id,Guid depositId,BookingRefundRequest request,
        [FromHeader(Name="Idempotency-Key")] string? key,CancellationToken ct)
    {
        var result=await service.RecordAsync(customerId,id,depositId,new(request.Version!.Value,request.Amount!.Value,request.CurrencyCode,
            request.ReturnedAtUtc!.Value,request.Source,request.Reference,request.Notes),key??"",ct);
        return result.Status == CommandStatus.Success?CreatedAtAction(nameof(Get),new {customerId,id,depositId,refundId=result.Value!.Id},result.Value):Map(result);
    }
    [HttpGet("api/crm/customers/{customerId:guid}/bookings/{id:guid}/deposits/{depositId:guid}/refunds")]
    public async Task<IActionResult> List(Guid customerId,Guid id,Guid depositId,CancellationToken ct,int page=1,int pageSize=20)=>Map(await service.ListAsync(customerId,id,depositId,page,pageSize,ct));
    [HttpGet("api/crm/customers/{customerId:guid}/bookings/{id:guid}/deposits/{depositId:guid}/refunds/{refundId:guid}")]
    public async Task<IActionResult> Get(Guid customerId,Guid id,Guid depositId,Guid refundId,CancellationToken ct)=>Map(await service.GetAsync(customerId,id,depositId,refundId,ct));
    private IActionResult Map<T>(BookingResult<T> result)=>result.Status switch {
        CommandStatus.Success=>Ok(result.Value),CommandStatus.Forbidden=>Forbid(),
        CommandStatus.NotFound=>NotFound(new ProblemDetails{Status=404,Title="Booking financial resource was not found."}),
        CommandStatus.Invalid=>BadRequest(new ProblemDetails{Status=400,Title="Invalid actual original-currency refund or query."}),
        CommandStatus.Conflict=>Conflict(new ProblemDetails{Status=409,Title="Booking version, recorded principal remainder or idempotency key conflicts. Reload before recording a new refund."}),
        _=>Problem(statusCode:503,title:"Booking refunds are unavailable.") };
}
