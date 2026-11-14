using BrokerHub.Api.Configuration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Controllers;

[ApiController,AllowAnonymous,Route("api/session")]
public sealed class SessionController(IAntiforgery antiforgery):ControllerBase
{
    [HttpGet("antiforgery")]
    public IActionResult Antiforgery()=>Request.Query.Count>0
        ?BadRequest(new ProblemDetails{Status=400,Title="The session token endpoint accepts no query parameters."})
        :Ok(new{Token=antiforgery.GetAndStoreTokens(HttpContext).RequestToken,HeaderName=NotificationApiSecurity.HeaderName});
}
