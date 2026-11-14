using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Bookings;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BrokerHub.Api.Controllers;
[ApiController, PrivateCrmResponse, Authorize(Policy = AuthorizationPolicies.Customer), Route("api/bookings")]
public sealed class BookingsController(IBookingService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitBookingRequest request, [FromHeader(Name="Idempotency-Key")] string? key, CancellationToken ct)
    {
        var result = await service.SubmitAsync(new(request.UnitId!.Value,request.RequestReason),key ?? "",ct);
        return result.Status == CommandStatus.Success ? CreatedAtAction(nameof(Own),new { id = result.Value!.Id },result.Value) : Map(result);
    }
    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct, int page=1, int pageSize=20) => Map(await service.MineAsync(page,pageSize,ct));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Own(Guid id, CancellationToken ct) => Map(await service.OwnAsync(id,ct));
    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken ct, int page=1, int pageSize=20) => Map(await service.OwnHistoryAsync(id,page,pageSize,ct));
    [HttpGet("{id:guid}/history/{eventId:guid}")]
    public async Task<IActionResult> Event(Guid id, Guid eventId, CancellationToken ct) => Map(await service.OwnEventAsync(id,eventId,ct));
    private IActionResult Map<T>(BookingResult<T> result) => result.Status switch {
        CommandStatus.Success => Ok(result.Value), CommandStatus.Forbidden => Forbid(),
        CommandStatus.NotFound => NotFound(new ProblemDetails { Status=404, Title="Booking resource was not found." }),
        CommandStatus.Invalid => BadRequest(new ProblemDetails { Status=400, Title="Invalid booking request." }),
        CommandStatus.Conflict => Conflict(new ProblemDetails { Status=409, Title="An active request, current eligibility or idempotency key conflicts. Reload before retrying." }),
        _ => Problem(statusCode:503,title:"Bookings are unavailable.") };
}
