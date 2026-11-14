using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Viewings;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;
[ApiController, Route("api/crm/customers/{customerId:guid}/viewings"), Authorize(Policy = AuthorizationPolicies.SalesStaff), PrivateCrmResponse]
public sealed class ViewingCoordinationController(IViewingCoordinationService service) : ControllerBase
{
    [HttpGet("/api/crm/viewings")] public async Task<IActionResult> Queue(CancellationToken ct, int page = 1, int pageSize = 20, string? status = null) => Map(await service.ListAsync(null, page, pageSize, status, ct));
    [HttpGet] public async Task<IActionResult> List(Guid customerId, CancellationToken ct, int page = 1, int pageSize = 20, string? status = null) => Map(await service.ListAsync(customerId, page, pageSize, status, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid customerId, Guid id, CancellationToken ct) => Map(await service.GetAsync(customerId, id, ct));
    [HttpGet("{id:guid}/history")] public async Task<IActionResult> History(Guid customerId, Guid id, CancellationToken ct, int page = 1, int pageSize = 20) => Map(await service.HistoryAsync(customerId, id, page, pageSize, ct));
    [HttpGet("{id:guid}/history/{eventId:guid}")] public async Task<IActionResult> Event(Guid customerId, Guid id, Guid eventId, CancellationToken ct) => Map(await service.EventAsync(customerId, id, eventId, ct));
    [HttpPost("{id:guid}/confirm")] public async Task<IActionResult> Confirm(Guid customerId, Guid id, ConfirmViewingRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct) => Map(await service.ConfirmAsync(customerId, id,
            new(request.Version!.Value, request.ConfirmedStartUtc!.Value, request.ConfirmedEndUtc!.Value,
                request.CoordinatedAtUtc!.Value, request.CoordinationSource, request.CoordinationReference), key ?? "", ct));
    private IActionResult Map<T>(ViewingResult<T> result) => result.Status switch
    {
        CommandStatus.Success => Ok(result.Value), CommandStatus.Invalid => BadRequest(new ProblemDetails { Status = 400, Title = "Invalid coordinated viewing request." }),
        CommandStatus.Forbidden => Forbid(), CommandStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "CRM viewing resource was not found." }),
        CommandStatus.Conflict => Conflict(new ProblemDetails { Status = 409, Title = "Viewing state, responsibility, pending cutoff, interval or idempotency key conflicts." }),
        _ => Problem(statusCode: 503, title: "Viewing coordination is unavailable.")
    };
}
