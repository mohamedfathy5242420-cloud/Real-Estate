using BrokerHub.Api.Authorization;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BrokerHub.Api.Controllers;
[ApiController,PrivateCrmResponse,Authorize(Policy=AuthorizationPolicies.Customer)]
public sealed class OwnBookingDepositsController(IBookingDepositService service) : ControllerBase
{
    [HttpGet("api/bookings/{id:guid}/deposits")]
    public async Task<IActionResult> List(Guid id, CancellationToken ct, int page=1, int pageSize=20) => Map(await service.OwnListAsync(id,page,pageSize,ct));
    [HttpGet("api/bookings/{id:guid}/deposits/{depositId:guid}")]
    public async Task<IActionResult> Get(Guid id, Guid depositId, CancellationToken ct) => Map(await service.OwnAsync(id,depositId,ct));
    private IActionResult Map<T>(BookingResult<T> result) => result.Status switch {
        CommandStatus.Success=>Ok(result.Value),CommandStatus.Forbidden=>Forbid(),
        CommandStatus.NotFound=>NotFound(new ProblemDetails{Status=404,Title="Booking resource was not found."}),
        CommandStatus.Invalid=>BadRequest(new ProblemDetails{Status=400,Title="Invalid deposit query."}),
        _=>Problem(statusCode:503,title:"Booking deposits are unavailable.") };
}
