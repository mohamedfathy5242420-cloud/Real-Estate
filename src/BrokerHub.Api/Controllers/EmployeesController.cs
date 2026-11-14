using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Employees;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Employees;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController, Route("api/employees"), Authorize(Policy = AuthorizationPolicies.SystemAdministrator), PrivateCrmResponse]
public sealed class EmployeesController(IEmployeeProfileService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeProfileRequest command, CancellationToken ct)
    {
        var result = await service.CreateAsync(new(command.AccountId!.Value, command.EmployeeNumber, command.DisplayName), ct);
        return result.Status == EmployeeProfileStatus.Success ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : Map(result);
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Map(await service.GetAsync(id, ct));
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct, int page = 1, int pageSize = 20, bool? isActive = null) => Map(await service.GetPageAsync(page, pageSize, isActive, ct));
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateEmployeeProfileRequest command, CancellationToken ct) =>
        Map(await service.UpdateAsync(id, new(command.DisplayName, command.IsActive!.Value, command.Version!.Value), ct));
    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken ct, int page = 1, int pageSize = 20) => Map(await service.GetHistoryAsync(id, page, pageSize, ct));
    private IActionResult Map<T>(EmployeeProfileResult<T> result) => result.Status switch
    {
        EmployeeProfileStatus.Success => Ok(result.Value),
        EmployeeProfileStatus.Invalid => BadRequest(new ProblemDetails { Status = 400, Title = "Invalid employee profile request." }),
        EmployeeProfileStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "Employee profile was not found." }),
        EmployeeProfileStatus.Conflict => Conflict(new ProblemDetails { Status = 409, Title = "Employee eligibility or version changed." }),
        _ => Forbid()
    };
}
