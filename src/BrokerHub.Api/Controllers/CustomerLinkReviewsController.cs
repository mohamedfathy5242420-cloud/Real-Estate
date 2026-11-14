using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Customers;
using BrokerHub.Api.Filters;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController, Route("api/customer-link-reviews"), Authorize(Policy = AuthorizationPolicies.SalesManager), CustomerProfileResponse]
public sealed class CustomerLinkReviewsController(ICustomerLinkService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Pending(CancellationToken ct, int page = 1, int pageSize = 20) => CustomerLinkResponse.Map(this, await service.GetPendingPageAsync(page, pageSize, ct));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => CustomerLinkResponse.Map(this, await service.GetReviewAsync(id, ct));
    [HttpGet("candidates")]
    public async Task<IActionResult> Candidates(CancellationToken ct, int page = 1, int pageSize = 20) => CustomerLinkResponse.Map(this, await service.GetCandidatesAsync(page, pageSize, ct));
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, ApproveCustomerLinkRequest command, CancellationToken ct) =>
        CustomerLinkResponse.Map(this, await service.DecideAsync(id, true, new(command.Version!.Value, command.Reason, command.CustomerId, command.CustomerVersion), ct));
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, RejectCustomerLinkRequest command, CancellationToken ct) =>
        CustomerLinkResponse.Map(this, await service.DecideAsync(id, false, new(command.Version!.Value, command.Reason), ct));
}
