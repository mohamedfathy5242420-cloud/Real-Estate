using BrokerHub.Api.Authorization;
using BrokerHub.Api.Contracts.Bookings;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BrokerHub.Api.Controllers;
[ApiController,PrivateCrmResponse,Authorize(Policy=AuthorizationPolicies.Customer)]
public sealed class BookingCancellationsController(IBookingCancellationService service) : ControllerBase
{
    [HttpPost("api/bookings/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancelBookingRequest request,[FromHeader(Name="Idempotency-Key")] string? key,CancellationToken ct)
    {
        var result = await service.CancelAsync(id,new(request.Version!.Value,request.Reason),key ?? "",ct);
        return result.Status switch {
            CommandStatus.Success => Ok(result.Value),CommandStatus.Forbidden => Forbid(),
            CommandStatus.NotFound => NotFound(new ProblemDetails{Status=404,Title="Booking resource was not found."}),
            CommandStatus.Invalid => BadRequest(new ProblemDetails{Status=400,Title="Invalid booking cancellation."}),
            CommandStatus.Conflict => Conflict(new ProblemDetails{Status=409,Title="Booking state, version, current cutoff or idempotency key conflicts. Reload before retrying."}),
            _ => Problem(statusCode:503,title:"Booking cancellation is unavailable.") };
    }
}
