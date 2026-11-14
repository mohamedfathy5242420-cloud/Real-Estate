using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Customers;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;
[ApiController, Route("api/crm/customers"), Authorize(Policy = AuthorizationPolicies.SalesStaff), PrivateCrmResponse]
public sealed class CrmFollowUpController(ICrmFollowUpService service) : ControllerBase
{
    [HttpPost, Authorize(Policy = AuthorizationPolicies.SalesManager)]
    public async Task<IActionResult> Intake(IntakeCustomerRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct)
    {
        var result = await service.IntakeAsync(new(request.FullName, request.Phone, request.Email, request.PreferredContactMethod, request.Requirements), key ?? "", ct);
        return result.Status == CrmStatus.Success ? CreatedAtAction("Get", "CrmCustomers", new { id = result.Value!.CustomerId }, result.Value) : Map(result);
    }
    [HttpGet("/api/crm/follow-ups")]
    public async Task<IActionResult> Queue(CancellationToken ct, int page = 1, int pageSize = 20, bool? hasRequirements = null, bool? hasNotes = null) => Map(await service.QueueAsync(page, pageSize, hasRequirements, hasNotes, ct));
    [HttpPut("{customerId:guid}/requirements")]
    public async Task<IActionResult> Requirements(Guid customerId, ChangeCustomerNeedsRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct) =>
        Map(await service.ChangeNeedsAsync(customerId, new(request.Version!.Value, request.Requirements), key ?? "", ct));
    [HttpGet("{customerId:guid}/needs-history")]
    public async Task<IActionResult> NeedsHistory(Guid customerId, CancellationToken ct, int page = 1, int pageSize = 20) => Map(await service.NeedsHistoryAsync(customerId, page, pageSize, ct));
    [HttpGet("{customerId:guid}/needs-history/{changeId:guid}")]
    public async Task<IActionResult> NeedsChange(Guid customerId, Guid changeId, CancellationToken ct) => Map(await service.NeedsChangeAsync(customerId, changeId, ct));
    [HttpGet("{customerId:guid}/notes")]
    public async Task<IActionResult> Notes(Guid customerId, CancellationToken ct, int page = 1, int pageSize = 20, Guid? leadId = null) => Map(await service.NotesAsync(customerId, leadId, page, pageSize, ct));
    [HttpGet("{customerId:guid}/notes/{noteId:guid}")]
    public async Task<IActionResult> Note(Guid customerId, Guid noteId, CancellationToken ct) => Map(await service.NoteAsync(customerId, noteId, ct));
    [HttpPost("{customerId:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid customerId, AddSalesNoteRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct)
    {
        var result = await service.AddNoteAsync(customerId, null, new(request.Body, request.LeadId), key ?? "", ct);
        return result.Status == CrmStatus.Success ? CreatedAtAction(nameof(Note), new { customerId, noteId = result.Value!.Id }, result.Value) : Map(result);
    }
    [HttpPost("{customerId:guid}/notes/{noteId:guid}/corrections")]
    public async Task<IActionResult> Correct(Guid customerId, Guid noteId, CorrectSalesNoteRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct)
    {
        var result = await service.AddNoteAsync(customerId, noteId, new(request.Body), key ?? "", ct);
        return result.Status == CrmStatus.Success ? CreatedAtAction(nameof(Note), new { customerId, noteId = result.Value!.Id }, result.Value) : Map(result);
    }
    private IActionResult Map<T>(CrmResult<T> result) => result.Status switch
    {
        CrmStatus.Success => Ok(result.Value), CrmStatus.Invalid => BadRequest(new ProblemDetails { Status = 400, Title = "Invalid CRM follow-up request." }),
        CrmStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "CRM resource was not found." }),
        CrmStatus.Conflict => Conflict(new ProblemDetails { Status = 409, Title = "CRM version or command changed." }), _ => Forbid()
    };
}
