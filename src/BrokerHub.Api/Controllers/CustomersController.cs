using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Customers;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Customers;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Policy = AuthorizationPolicies.Customer)]
[CustomerProfileResponse]
public sealed class CustomersController(ICustomerProfileService profiles) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerProfileRequest request, CancellationToken ct)
    {
        var result = await profiles.CreateOwnAsync(new(request.FullName, request.Phone, request.Email,
            request.PreferredContactMethod, request.Requirements), ct);
        return result.Status == CustomerProfileStatus.Success
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : Map(result);
    }
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) => Map(await profiles.GetOwnAsync(ct));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Map(await profiles.GetByIdAsync(id, ct));
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerProfileRequest request, CancellationToken ct) =>
        Map(await profiles.UpdateAsync(id, new(request.FullName, request.Phone, request.Email,
            request.PreferredContactMethod, request.Requirements, request.Version!.Value), ct));

    private IActionResult Map(CustomerProfileResult<CustomerProfileDto> result) => result.Status switch
    {
        CustomerProfileStatus.Success => Ok(result.Value),
        CustomerProfileStatus.Invalid => BadRequest(new ValidationProblemDetails(result.Errors!.ToDictionary(p => p.Key, p => p.Value))),
        CustomerProfileStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "Customer profile was not found." }),
        CustomerProfileStatus.Conflict => Conflict(new ProblemDetails { Status = 409, Title = "Customer profile changed or already exists. Reload before retrying." }),
        CustomerProfileStatus.Forbidden => Forbid(),
        _ => Problem(statusCode: 503, title: "Customer profile is unavailable.")
    };
}
