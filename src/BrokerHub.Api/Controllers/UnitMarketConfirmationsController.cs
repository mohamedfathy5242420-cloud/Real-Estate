using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.UnitMarketConfirmations;
using BrokerHub.Application.DTOs.Units;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BrokerHub.Api.Controllers;
[ApiController]
[Route("api/units/{unitId:guid}/market-confirmations")]
[Authorize(Policy = AuthorizationPolicies.SystemAdministrator)]
public sealed class UnitMarketConfirmationsController(IUnitMarketConfirmationService confirmations) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Record(Guid unitId, RecordUnitMarketConfirmationRequest request, CancellationToken ct)
    {
        var r = await confirmations.RecordAsync(unitId, new(request.ConfirmedAtUtc!.Value, request.Source,
            request.SourceReference, request.Notes, request.ConfirmedPrice, request.Currency, request.AvailabilityStatus,
            request.Version!.Value), ct);
        return r.Status == UnitServiceStatus.Success ? StatusCode(201, r.Value) : Failure(r);
    }
    [HttpGet]
    public async Task<IActionResult> List(Guid unitId, CancellationToken ct, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var r = await confirmations.ListAsync(unitId, skip, take, ct);
        return r.Status == UnitServiceStatus.Success ? Ok(r.Value) : Failure(r);
    }
    private IActionResult Failure<T>(UnitServiceResult<T> r) => r.Status switch
    {
        UnitServiceStatus.ValidationFailed => BadRequest(new ValidationProblemDetails(r.Errors.ToDictionary(p => p.Key, p => p.Value))),
        UnitServiceStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "Unit was not found." }),
        UnitServiceStatus.Conflict => Conflict(new ProblemDetails { Status = 409, Title = "Unit confirmation conflict.", Detail = string.Join(" ",r.Errors.Values.SelectMany(v => v)) }),
        UnitServiceStatus.Forbidden => Forbid(),
        _ => Problem(statusCode:500)
    };
}
