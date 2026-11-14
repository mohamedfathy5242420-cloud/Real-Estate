using BrokerHub.Api.Configuration;
using BrokerHub.Api.Filters;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Notifications;
using BrokerHub.Application.Interfaces.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController,Authorize,PrivateCrmResponse,Route("api/notifications")]
public sealed class NotificationsController(IUserNotificationService service,IAntiforgery antiforgery):ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct,int page=1,int pageSize=20,string? readStatus=null)=>
        BadQuery("page","pageSize","readStatus")?Invalid():Map(await service.ListAsync(new(page,pageSize,readStatus),ct));
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)=>BadQuery()?Invalid():Map(await service.UnreadCountAsync(ct));
    [HttpGet("antiforgery")]
    public async Task<IActionResult> Antiforgery(CancellationToken ct)
    {
        if(BadQuery())return Invalid();if(!await service.CanAccessAsync(ct))return Forbid();
        return Ok(new{Token=antiforgery.GetAndStoreTokens(HttpContext).RequestToken,HeaderName=NotificationApiSecurity.HeaderName});
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id,CancellationToken ct)=>BadQuery()?Invalid():Map(await service.GetAsync(id,ct));
    [HttpPut("{id:guid}/read"),RequestSizeLimit(1024)]
    public async Task<IActionResult> Read(Guid id,CancellationToken ct)
    {
        if(Request.ContentLength>1024)return StatusCode(413);
        try{await antiforgery.ValidateRequestAsync(HttpContext);}
        catch(AntiforgeryValidationException){return BadRequest(new ProblemDetails{Status=400,Title="Notification request verification failed."});}
        catch(BadHttpRequestException ex)when(ex.StatusCode==413){return StatusCode(413);}
        if(BadQuery())return Invalid();
        var first=new byte[1];if(await Request.Body.ReadAsync(first,ct)>0)return Invalid();
        return Map(await service.MarkReadAsync(id,ct));
    }
    private bool BadQuery(params string[] allowed)=>Request.Query.Any(q=>!allowed.Contains(q.Key,StringComparer.Ordinal)||q.Value.Count!=1||string.IsNullOrWhiteSpace(q.Value[0]));
    private BadRequestObjectResult Invalid()=>BadRequest(new ProblemDetails{Status=400,Title="Invalid notification query, identity or request body."});
    private IActionResult Map<T>(NotificationResult<T> result)=>result.Status switch
    {
        CommandStatus.Success=>Ok(result.Value),CommandStatus.Forbidden=>Forbid(),CommandStatus.Invalid=>Invalid(),
        CommandStatus.NotFound=>NotFound(new ProblemDetails{Status=404,Title="Notification was not found."}),
        CommandStatus.Conflict=>Conflict(new ProblemDetails{Status=409,Title="Notification state conflicts. Reload before retrying."}),
        _=>Problem(statusCode:503,title:"Notifications are temporarily unavailable.")
    };
}
