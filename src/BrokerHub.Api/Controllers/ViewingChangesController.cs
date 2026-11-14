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
public sealed class ViewingChangesController(IViewingChangeService service) : ControllerBase
{
    [HttpPost("api/viewings/{id:guid}/cancel"), Authorize(Policy = AuthorizationPolicies.Customer)]
    public async Task<IActionResult> CancelOwn(Guid id, CancelViewingRequest request, [FromHeader(Name="Idempotency-Key")] string? key, CancellationToken ct) =>
        Map(await service.CancelOwnAsync(id, new(request.Version!.Value, request.Reason), key ?? "", ct));
    [HttpPost("api/viewings/{id:guid}/change-proposal"), Authorize(Policy = AuthorizationPolicies.Customer)]
    public async Task<IActionResult> Change(Guid id, ChangeViewingProposalRequest request, [FromHeader(Name="Idempotency-Key")] string? key, CancellationToken ct) =>
        Map(await service.ChangeProposalAsync(id, new(request.Version!.Value, request.ProposedStartUtc!.Value, request.Reason), key ?? "", ct));
    [HttpPost("api/crm/customers/{customerId:guid}/viewings/{id:guid}/cancel"), Authorize(Policy = AuthorizationPolicies.SalesStaff)]
    public async Task<IActionResult> CancelStaff(Guid customerId, Guid id, CancelStaffViewingRequest request, [FromHeader(Name="Idempotency-Key")] string? key, CancellationToken ct) =>
        Map(await service.CancelStaffAsync(customerId, id, new(request.Version!.Value, request.CancellationKind, request.Reason), key ?? "", ct));
    private IActionResult Map<T>(ViewingResult<T> result) => result.Status switch
    {
        CommandStatus.Success => Ok(result.Value), CommandStatus.Invalid => BadRequest(new ProblemDetails{Status=400,Title="Invalid viewing change."}),
        CommandStatus.Forbidden => Forbid(), CommandStatus.NotFound => NotFound(new ProblemDetails{Status=404,Title="Viewing resource was not found."}),
        CommandStatus.Conflict => Conflict(new ProblemDetails{Status=409,Title="Viewing state, version, current cutoff or idempotency key conflicts. Reload before retrying."}),
        _ => Problem(statusCode:503,title:"Viewing changes are unavailable.")
    };
}
