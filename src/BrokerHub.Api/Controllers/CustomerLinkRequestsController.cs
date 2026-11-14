using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Customers;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Customers;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController, Route("api/customer-link-requests"), Authorize(Policy = AuthorizationPolicies.Customer), CustomerProfileResponse]
public sealed class CustomerLinkRequestsController(ICustomerLinkService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitCustomerLinkRequest command, CancellationToken ct)
    {
        var result = await service.SubmitAsync(command.Claim, ct);
        return result.Status == CustomerLinkStatus.Success ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : CustomerLinkResponse.Map(this, result);
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => CustomerLinkResponse.Map(this, await service.GetOwnAsync(id, ct));
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct, int page = 1, int pageSize = 20) => CustomerLinkResponse.Map(this, await service.GetOwnPageAsync(page, pageSize, ct));
}

internal static class CustomerLinkResponse
{
    internal static IActionResult Map<T>(ControllerBase controller, CustomerLinkResult<T> result) => result.Status switch
    {
        CustomerLinkStatus.Success => controller.Ok(result.Value),
        CustomerLinkStatus.Invalid => controller.BadRequest(new ProblemDetails { Status = 400, Title = "Invalid link request." }),
        CustomerLinkStatus.NotFound => controller.NotFound(new ProblemDetails { Status = 404, Title = "Link request was not found." }),
        CustomerLinkStatus.Conflict => controller.Conflict(new ProblemDetails { Status = 409, Title = "Link request eligibility or version changed." }),
        _ => controller.Forbid()
    };
}
