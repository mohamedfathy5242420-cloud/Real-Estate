using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Customers;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;
[ApiController, Route("api/crm/customers/{customerId:guid}/leads"), Authorize(Policy = AuthorizationPolicies.SalesStaff), PrivateCrmResponse]
public sealed class LeadsController(ILeadService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(Guid customerId, CancellationToken ct, int page = 1, int pageSize = 20, string? status = null) => Map(await service.ListAsync(customerId, page, pageSize, status, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid customerId, Guid id, CancellationToken ct) => Map(await service.GetAsync(customerId, id, ct));
    [HttpGet("{id:guid}/history")] public async Task<IActionResult> History(Guid customerId, Guid id, CancellationToken ct, int page = 1, int pageSize = 20) => Map(await service.HistoryAsync(customerId, id, page, pageSize, ct));
    [HttpGet("{id:guid}/history/{eventId:guid}")] public async Task<IActionResult> Event(Guid customerId, Guid id, Guid eventId, CancellationToken ct) => Map(await service.EventAsync(customerId, id, eventId, ct));
    [HttpPost] public async Task<IActionResult> Open(Guid customerId, OpenLeadRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct)
    {
        var result = await service.OpenAsync(customerId, new(request.ProjectId!.Value, request.Source), key ?? "", ct);
        return result.Status == CrmStatus.Success && result.Value!.Created ? CreatedAtAction(nameof(Get), new { customerId, id = result.Value.Id }, result.Value) : Map(result);
    }
    [HttpPost("{id:guid}/close")] public async Task<IActionResult> Close(Guid customerId, Guid id, CloseLeadRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct) =>
        Map(await service.CloseAsync(customerId, id, new(request.Version!.Value, request.Reason), key ?? "", ct));
    private IActionResult Map<T>(CrmResult<T> result) => result.Status switch
    {
        CrmStatus.Success => Ok(result.Value), CrmStatus.Invalid => BadRequest(new ProblemDetails { Status = 400, Title = "Invalid Lead request." }),
        CrmStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "CRM resource was not found." }),
        CrmStatus.Conflict => Conflict(new ProblemDetails { Status = 409, Title = "Lead state, version or original agreement eligibility changed." }), _ => Forbid()
    };
}
