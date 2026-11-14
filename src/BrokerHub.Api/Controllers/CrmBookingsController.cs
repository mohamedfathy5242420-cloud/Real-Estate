using BrokerHub.Api.Authorization;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Bookings;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BrokerHub.Api.Controllers;
[ApiController, PrivateCrmResponse, Authorize(Policy = AuthorizationPolicies.SalesStaff)]
public sealed class CrmBookingsController(IBookingService service) : ControllerBase
{
    [HttpGet("api/crm/bookings")]
    public async Task<IActionResult> List(CancellationToken ct, int page=1, int pageSize=20, string? status=null) => Map(await service.StaffListAsync(null,status,page,pageSize,ct));
    [HttpGet("api/crm/customers/{customerId:guid}/bookings")]
    public async Task<IActionResult> CustomerList(Guid customerId, CancellationToken ct, int page=1, int pageSize=20, string? status=null) => Map(await service.StaffListAsync(customerId,status,page,pageSize,ct));
    [HttpGet("api/crm/customers/{customerId:guid}/bookings/{id:guid}")]
    public async Task<IActionResult> Get(Guid customerId, Guid id, CancellationToken ct) => Map(await service.StaffAsync(customerId,id,ct));
    [HttpGet("api/crm/customers/{customerId:guid}/bookings/{id:guid}/history")]
    public async Task<IActionResult> History(Guid customerId, Guid id, CancellationToken ct, int page=1, int pageSize=20) => Map(await service.StaffHistoryAsync(customerId,id,page,pageSize,ct));
    [HttpGet("api/crm/customers/{customerId:guid}/bookings/{id:guid}/history/{eventId:guid}")]
    public async Task<IActionResult> Event(Guid customerId, Guid id, Guid eventId, CancellationToken ct) => Map(await service.StaffEventAsync(customerId,id,eventId,ct));
    private IActionResult Map<T>(BookingResult<T> result) => result.Status switch {
        CommandStatus.Success => Ok(result.Value), CommandStatus.Forbidden => Forbid(),
        CommandStatus.NotFound => NotFound(new ProblemDetails { Status=404, Title="Booking resource was not found." }),
        CommandStatus.Invalid => BadRequest(new ProblemDetails { Status=400, Title="Invalid booking query." }),
        _ => Problem(statusCode:503,title:"Bookings are unavailable.") };
}
