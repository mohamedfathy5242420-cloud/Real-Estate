using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.MarketingAgreements;
using BrokerHub.Application.DTOs.MarketingAgreements;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController]
[Route("api/marketing-agreements")]
[Authorize(Policy = AuthorizationPolicies.SystemAdministrator)]
[RequestSizeLimit(262144)]
public sealed class MarketingAgreementsController(IMarketingAgreementService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateMarketingAgreementRequest request, CancellationToken ct)
    {
        var r = await service.CreateAsync(new CreateMarketingAgreementDto(request.DeveloperId, request.AgreementNumber,
            request.ScopeType, request.StartsOn!.Value, request.EndsOn!.Value, request.ProtectionPeriodDays, request.ProjectIds, request.CommissionRules), ct);
        return r.Status == MarketingAgreementStatus.Success ? CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value) : Failure(r);
    }
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? developerId, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var r = await service.ListAsync(developerId, skip, take, ct);
        return r.Status == MarketingAgreementStatus.Success ? Ok(r.Value) : Failure(r);
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await service.GetByIdAsync(id, ct);
        return r.Status == MarketingAgreementStatus.Success ? Ok(r.Value) : Failure(r);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateMarketingAgreementRequest request, CancellationToken ct)
    {
        var r = await service.UpdateAsync(id, new UpdateMarketingAgreementDto(request.AgreementNumber, request.ScopeType,
            request.StartsOn!.Value, request.EndsOn!.Value, request.ProtectionPeriodDays, request.ProjectIds, request.CommissionRules, request.Version!.Value), ct);
        return r.Status == MarketingAgreementStatus.Success ? Ok(r.Value) : Failure(r);
    }
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, ActivateMarketingAgreementRequest request, CancellationToken ct)
    {
        var r = await service.ActivateAsync(id, request.Version!.Value, ct);
        return r.Status == MarketingAgreementStatus.Success ? Ok(r.Value) : Failure(r);
    }
    private IActionResult Failure<T>(MarketingAgreementResult<T> r) => r.Status switch
    {
        MarketingAgreementStatus.ValidationFailed => BadRequest(new ValidationProblemDetails(r.Errors.ToDictionary(p => p.Key, p => p.Value))),
        MarketingAgreementStatus.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "Agreement resource not found", Detail = string.Join(" ", r.Errors.Values.SelectMany(v => v)) }),
        MarketingAgreementStatus.Conflict => Conflict(new ProblemDetails { Status = 409, Title = "Agreement conflict", Detail = string.Join(" ", r.Errors.Values.SelectMany(v => v)) }),
        MarketingAgreementStatus.Forbidden => Forbid(),
        _ => Problem(statusCode: 500)
    };
}
