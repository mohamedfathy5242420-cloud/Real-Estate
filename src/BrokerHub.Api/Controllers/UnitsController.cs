using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Units;
using BrokerHub.Application.DTOs.Units;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController]
[Route("api/units")]
[Authorize(Policy = AuthorizationPolicies.SystemAdministrator)]
public sealed class UnitsController(IUnitService unitService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await unitService.CreateAsync(new CreateUnitDto(request.ProjectId, request.Code,
            request.Title, request.Description, request.UnitType, request.AreaSqm, request.Bedrooms,
            request.Bathrooms, request.ListPrice, request.Currency), cancellationToken);
        return result.Status == UnitServiceStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) : MapFailure(result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? projectId, CancellationToken cancellationToken)
    {
        var result = await unitService.ListAsync(projectId, cancellationToken);
        return result.Status == UnitServiceStatus.Success ? Ok(result.Value) : MapFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await unitService.GetByIdAsync(id, cancellationToken);
        return result.Status == UnitServiceStatus.Success ? Ok(result.Value) : MapFailure(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await unitService.UpdateAsync(id, new UpdateUnitDto(request.Title, request.Description,
            request.UnitType, request.AreaSqm, request.Bedrooms, request.Bathrooms, request.ListPrice,
            request.Currency, request.Version), cancellationToken);
        return result.Status == UnitServiceStatus.Success ? Ok(result.Value) : MapFailure(result);
    }

    private IActionResult MapFailure<T>(UnitServiceResult<T> result)
    {
        var errors = result.Errors.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var detail = string.Join(" ", errors.SelectMany(pair => pair.Value));
        return result.Status switch
        {
            UnitServiceStatus.ValidationFailed => BadRequest(new ValidationProblemDetails(errors)),
            UnitServiceStatus.NotFound => NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound, Title = "Unit resource was not found.", Detail = detail
            }),
            UnitServiceStatus.Conflict => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict, Title = "Unit conflict.", Detail = detail
            }),
            UnitServiceStatus.Forbidden => Forbid(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
