using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Viewings;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;
[ApiController, Route("api/viewings"), Authorize(Policy = AuthorizationPolicies.Customer), PrivateCrmResponse]
public sealed class ViewingsController(IViewingRequestService service) : ControllerBase
{
    [HttpPost] public async Task<IActionResult> Submit(SubmitViewingRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct)
    {
        var result = await service.SubmitAsync(new(request.UnitId!.Value, request.ProposedStartUtc!.Value), key ?? "", ct);
        return result.Status == CommandStatus.Success ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : Map(result);
    }
    [HttpGet("mine")] public async Task<IActionResult> Mine(CancellationToken ct, int page = 1, int pageSize = 20) => Map(await service.MineAsync(page, pageSize, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Map(await service.GetAsync(id, ct));
    [HttpGet("{id:guid}/history")] public async Task<IActionResult> History(Guid id, CancellationToken ct, int page = 1, int pageSize = 20) => Map(await service.HistoryAsync(id, page, pageSize, ct));
    [HttpGet("{id:guid}/history/{eventId:guid}")] public async Task<IActionResult> Event(Guid id, Guid eventId, CancellationToken ct) => Map(await service.EventAsync(id, eventId, ct));
    private IActionResult Map<T>(ViewingResult<T> result) => result.Status switch
    {
        CommandStatus.Success => Ok(result.Value), CommandStatus.Invalid => BadRequest(new ProblemDetails { Status = 400, Title = "Invalid viewing request." }),
        CommandStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "Viewing resource was not found." }),
        CommandStatus.Forbidden => Forbid(), CommandStatus.Conflict => Conflict(new ProblemDetails { Status = 409, Title = "Viewing request conflicts with current data or its idempotency key." }),
        _ => Problem(statusCode: 503, title: "Viewing requests are unavailable.")
    };
}
