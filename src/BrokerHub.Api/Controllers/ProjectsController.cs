using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Projects;
using BrokerHub.Application.DTOs.Projects;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize(Policy = AuthorizationPolicies.SystemAdministrator)]
public sealed class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.CreateAsync(
            new CreateProjectDto(
                request.DeveloperId,
                request.Code,
                request.Name,
                request.Area,
                request.Address,
                request.TimeZoneId),
            cancellationToken);

        return result.Status == ProjectServiceStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : MapFailure(result);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? developerId,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await projectService.ListAsync(developerId, includeInactive, cancellationToken);
        return result.Status == ProjectServiceStatus.Success
            ? Ok(result.Value)
            : MapFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await projectService.GetByIdAsync(id, cancellationToken);
        return result.Status == ProjectServiceStatus.Success
            ? Ok(result.Value)
            : MapFailure(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.UpdateAsync(
            id,
            new UpdateProjectDto(
                request.Name,
                request.Area,
                request.Address,
                request.TimeZoneId,
                request.Version),
            cancellationToken);

        return result.Status == ProjectServiceStatus.Success
            ? Ok(result.Value)
            : MapFailure(result);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        ChangeProjectStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.DeactivateAsync(id, request.Version, cancellationToken);
        return result.Status == ProjectServiceStatus.Success
            ? Ok(result.Value)
            : MapFailure(result);
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(
        Guid id,
        ChangeProjectStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.ReactivateAsync(id, request.Version, cancellationToken);
        return result.Status == ProjectServiceStatus.Success
            ? Ok(result.Value)
            : MapFailure(result);
    }

    private IActionResult MapFailure<T>(ProjectServiceResult<T> result)
    {
        var errors = result.Errors.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        var detail = string.Join(" ", errors.SelectMany(pair => pair.Value));

        return result.Status switch
        {
            ProjectServiceStatus.ValidationFailed =>
                BadRequest(new ValidationProblemDetails(errors)),
            ProjectServiceStatus.NotFound =>
                NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Project resource was not found.",
                    Detail = detail
                }),
            ProjectServiceStatus.Conflict =>
                Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Project conflict.",
                    Detail = detail
                }),
            ProjectServiceStatus.Forbidden => Forbid(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
