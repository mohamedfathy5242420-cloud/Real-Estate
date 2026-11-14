using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Viewings;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BrokerHub.Api.Controllers;
[ApiController, PrivateCrmResponse, Authorize(Policy = AuthorizationPolicies.SalesStaff)]
public sealed class ViewingOutcomesController(IViewingOutcomeService service) : ControllerBase
{
    [HttpPost("api/crm/customers/{customerId:guid}/viewings/{id:guid}/outcome")]
    public async Task<IActionResult> Record(Guid customerId, Guid id, ViewingOutcomeRequest request,
        [FromHeader(Name="Idempotency-Key")] string? key, CancellationToken ct)
    {
        var result = await service.RecordAsync(customerId, id, new(request.Version!.Value, request.Outcome,
            request.OccurredAtUtc!.Value, request.Notes), key ?? "", ct);
        return result.Status switch {
            CommandStatus.Success => Ok(result.Value),
            CommandStatus.Invalid => BadRequest(new ProblemDetails { Status=400, Title="Invalid viewing outcome." }),
            CommandStatus.Forbidden => Forbid(),
            CommandStatus.NotFound => NotFound(new ProblemDetails { Status=404, Title="Viewing resource was not found." }),
            CommandStatus.Conflict => Conflict(new ProblemDetails { Status=409, Title="Viewing state, version, actual occurrence or idempotency key conflicts. Reload before retrying." }),
            _ => Problem(statusCode:503, title:"Viewing outcomes are unavailable.") };
    }
}
