using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Units;
using BrokerHub.Application.DTOs.Units;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BrokerHub.Api.Controllers;
[ApiController]
[Route("api/units/{id:guid}")]
[Authorize(Policy = AuthorizationPolicies.SystemAdministrator)]
public sealed class UnitPublicationsController(IUnitPublicationService publications) : ControllerBase
{
    [HttpPost("publish")]
    public Task<IActionResult> Publish(Guid id, ChangeUnitPublicationRequest request, CancellationToken ct) => Change(id, true, request, ct);
    [HttpPost("unpublish")]
    public Task<IActionResult> Unpublish(Guid id, ChangeUnitPublicationRequest request, CancellationToken ct) => Change(id, false, request, ct);
    private async Task<IActionResult> Change(Guid id, bool publish, ChangeUnitPublicationRequest request, CancellationToken ct)
    {
        var r = await publications.ChangeAsync(id, publish, new(request.Version!.Value, request.Reason), ct);
        return r.Status switch
        {
            UnitServiceStatus.Success => Ok(r.Value),
            UnitServiceStatus.Forbidden => Forbid(),
            UnitServiceStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "Unit was not found." }),
            UnitServiceStatus.Conflict => Conflict(new ProblemDetails { Status = 409, Title = "Unit publication conflict.", Detail = string.Join(" ", r.Errors.Values.SelectMany(v => v)) }),
            UnitServiceStatus.ValidationFailed => BadRequest(new ValidationProblemDetails(r.Errors.ToDictionary(p => p.Key, p => p.Value))),
            _ => Problem(statusCode: 500)
        };
    }
}
