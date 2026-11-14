using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Developers;
using BrokerHub.Application.DTOs.Developers;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController]
[Route("api/developers")]
[Authorize(Policy = AuthorizationPolicies.SystemAdministrator)]
public sealed class DevelopersController(IDeveloperService developerService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateDeveloperRequest request,
        CancellationToken cancellationToken)
    {
        var result = await developerService.CreateAsync(
            new CreateDeveloperDto(
                request.Code,
                request.Name,
                request.LegalName,
                request.ExternalContactName,
                request.ExternalContactEmail,
                request.ExternalContactPhone),
            cancellationToken);

        return result.Status == DeveloperServiceStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : MapFailure(result);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await developerService.ListAsync(includeInactive, cancellationToken);
        return result.Status == DeveloperServiceStatus.Success
            ? Ok(result.Value)
            : MapFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await developerService.GetByIdAsync(id, cancellationToken);
        return result.Status == DeveloperServiceStatus.Success
            ? Ok(result.Value)
            : MapFailure(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateDeveloperRequest request,
        CancellationToken cancellationToken)
    {
        var result = await developerService.UpdateAsync(
            id,
            new UpdateDeveloperDto(
                request.Name,
                request.LegalName,
                request.ExternalContactName,
                request.ExternalContactEmail,
                request.ExternalContactPhone,
                request.Version),
            cancellationToken);

        return result.Status == DeveloperServiceStatus.Success
            ? Ok(result.Value)
            : MapFailure(result);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        ChangeDeveloperStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await developerService.DeactivateAsync(id, request.Version, cancellationToken);
        return result.Status == DeveloperServiceStatus.Success
            ? Ok(result.Value)
            : MapFailure(result);
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(
        Guid id,
        ChangeDeveloperStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await developerService.ReactivateAsync(id, request.Version, cancellationToken);
        return result.Status == DeveloperServiceStatus.Success
            ? Ok(result.Value)
            : MapFailure(result);
    }

    private IActionResult MapFailure<T>(DeveloperServiceResult<T> result)
    {
        var errors = result.Errors.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        var detail = string.Join(" ", errors.SelectMany(pair => pair.Value));

        return result.Status switch
        {
            DeveloperServiceStatus.ValidationFailed =>
                BadRequest(new ValidationProblemDetails(errors)),
            DeveloperServiceStatus.NotFound =>
                NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Developer was not found.",
                    Detail = detail
                }),
            DeveloperServiceStatus.Conflict =>
                Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Developer conflict.",
                    Detail = detail
                }),
            DeveloperServiceStatus.Forbidden => Forbid(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
