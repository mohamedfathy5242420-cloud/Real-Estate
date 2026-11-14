using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Viewings;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Viewings;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;
[ApiController, PrivateCrmResponse]
public sealed class ViewingAlternativesController(IViewingAlternativeService service) : ControllerBase
{
    [HttpPost("api/crm/customers/{customerId:guid}/viewings/{id:guid}/alternatives"), Authorize(Policy = AuthorizationPolicies.SalesStaff)]
    public async Task<IActionResult> Offer(Guid customerId, Guid id, OfferViewingAlternativeRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct) => Map(await service.OfferAsync(customerId, id,
            new(request.Version!.Value, request.ProposedStartUtc!.Value, request.ProposedEndUtc!.Value, request.CoordinatedAtUtc!.Value,
                request.CoordinationSource, request.CoordinationReference, request.Reason), key ?? "", ct));
    [HttpPost("api/viewings/{id:guid}/accept-alternative"), Authorize(Policy = AuthorizationPolicies.Customer)]
    public async Task<IActionResult> Accept(Guid id, AcceptViewingAlternativeRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct) => Map(await service.AcceptAsync(id, new(request.Version!.Value), key ?? "", ct));
    private IActionResult Map<T>(ViewingResult<T> result) => result.Status switch
    {
        CommandStatus.Success => Ok(result.Value), CommandStatus.Invalid => BadRequest(new ProblemDetails { Status = 400, Title = "Invalid viewing alternative request." }),
        CommandStatus.Forbidden => Forbid(), CommandStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "Viewing resource was not found." }),
        CommandStatus.Conflict => Conflict(new ProblemDetails { Status = 409, Title = "Viewing alternative, responsibility, cutoff, interval or idempotency key conflicts. Reload before retrying." }),
        _ => Problem(statusCode: 503, title: "Viewing alternatives are unavailable.")
    };
}
