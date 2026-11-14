using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Customers;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController, Route("api/crm/customers"), Authorize(Policy = AuthorizationPolicies.SalesStaff), PrivateCrmResponse]
public sealed class CrmCustomersController(ICustomerAssignmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct, int page = 1, int pageSize = 20) => Map(await service.GetPageAsync(page, pageSize, false, ct));
    [HttpGet("unassigned"), Authorize(Policy = AuthorizationPolicies.SalesManager)]
    public async Task<IActionResult> Unassigned(CancellationToken ct, int page = 1, int pageSize = 20) => Map(await service.GetPageAsync(page, pageSize, true, ct));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Map(await service.GetAsync(id, ct));
    [HttpGet("{id:guid}/assignments")]
    public async Task<IActionResult> History(Guid id, CancellationToken ct, int page = 1, int pageSize = 20) => Map(await service.GetHistoryAsync(id, page, pageSize, ct));
    [HttpGet("{id:guid}/assignments/{assignmentId:guid}")]
    public async Task<IActionResult> Assignment(Guid id, Guid assignmentId, CancellationToken ct) => Map(await service.GetAssignmentAsync(id, assignmentId, ct));
    [HttpGet("/api/crm/assignment-candidates"), Authorize(Policy = AuthorizationPolicies.SalesManager)]
    public async Task<IActionResult> Candidates(CancellationToken ct, int page = 1, int pageSize = 20) => Map(await service.GetCandidatesAsync(page, pageSize, ct));
    [HttpPost("{id:guid}/assign"), Authorize(Policy = AuthorizationPolicies.SalesManager)]
    public async Task<IActionResult> Assign(Guid id, AssignCustomerRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct)
    {
        var result = await service.ChangeAsync(id, false, new(request.EmployeeId!.Value, request.Version!.Value, request.Reason), key ?? "", ct);
        return result.Status == CrmStatus.Success ? CreatedAtAction(nameof(Assignment), new { id, assignmentId = result.Value!.Id }, result.Value) : Map(result);
    }
    [HttpPost("{id:guid}/transfer"), Authorize(Policy = AuthorizationPolicies.SalesManager)]
    public async Task<IActionResult> Transfer(Guid id, TransferCustomerRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct) =>
        Map(await service.ChangeAsync(id, true, new(request.EmployeeId!.Value, request.Version!.Value, request.Reason), key ?? "", ct));
    private IActionResult Map<T>(CrmResult<T> result) => result.Status switch
    {
        CrmStatus.Success => Ok(result.Value),
        CrmStatus.Invalid => BadRequest(new ProblemDetails { Status = 400, Title = "Invalid CRM request." }),
        CrmStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "CRM resource was not found." }),
        CrmStatus.Conflict => Conflict(new ProblemDetails { Status = 409, Title = "CRM assignment eligibility or version changed." }),
        _ => Forbid()
    };
}
