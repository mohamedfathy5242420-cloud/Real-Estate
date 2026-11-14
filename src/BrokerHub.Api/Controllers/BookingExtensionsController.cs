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
public sealed class BookingExtensionsController(IBookingExtensionService service) : ControllerBase
{
    [HttpPost("api/crm/customers/{customerId:guid}/bookings/{id:guid}/extensions")]
    public async Task<IActionResult> Record(Guid customerId, Guid id, BookingExtensionRequest request,
        [FromHeader(Name="Idempotency-Key")] string? key, CancellationToken ct)
    {
        var result = await service.RecordAsync(customerId,id,new(request.Version!.Value,request.DeveloperExtendedAtUtc!.Value,
            request.NewReservationExpiresAtUtc!.Value,request.Source,request.Reference,request.Reason),key ?? "",ct);
        return result.Status == CommandStatus.Success ? CreatedAtAction(nameof(Get),new {customerId,id,extensionId=result.Value!.Id},result.Value) : Map(result);
    }
    [HttpGet("api/crm/customers/{customerId:guid}/bookings/{id:guid}/extensions")]
    public async Task<IActionResult> List(Guid customerId, Guid id, CancellationToken ct, int page=1, int pageSize=20) => Map(await service.ListAsync(customerId,id,page,pageSize,ct));
    [HttpGet("api/crm/customers/{customerId:guid}/bookings/{id:guid}/extensions/{extensionId:guid}")]
    public async Task<IActionResult> Get(Guid customerId, Guid id, Guid extensionId, CancellationToken ct) => Map(await service.GetAsync(customerId,id,extensionId,ct));
    private IActionResult Map<T>(BookingResult<T> result) => result.Status switch {
        CommandStatus.Success => Ok(result.Value),CommandStatus.Forbidden => Forbid(),
        CommandStatus.Invalid => BadRequest(new ProblemDetails {Status=400,Title="Invalid actual reservation extension or query."}),
        CommandStatus.NotFound => NotFound(new ProblemDetails {Status=404,Title="Booking resource was not found."}),
        CommandStatus.Conflict => Conflict(new ProblemDetails {Status=409,Title="Booking state, version, deadline, current Unit claim or idempotency key conflicts. Reload before recording a new extension."}),
        _ => Problem(statusCode:503,title:"Booking extensions are unavailable.") };
}
