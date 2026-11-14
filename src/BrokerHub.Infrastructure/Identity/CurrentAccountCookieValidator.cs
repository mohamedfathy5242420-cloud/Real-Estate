using System.Security.Claims;
using BrokerHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BrokerHub.Infrastructure.Identity;

internal static class CurrentAccountCookieValidator
{
    internal static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var claims=context.HttpContext.RequestServices.GetRequiredService<IOptions<IdentityOptions>>().Value.ClaimsIdentity;
        var rawId=context.Principal?.FindFirstValue(claims.UserIdClaimType);
        if(!Guid.TryParse(rawId,out var id)||id==Guid.Empty){await Reject(context);return;}
        var db=context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();var ct=context.HttpContext.RequestAborted;
        var account=await db.Users.AsNoTracking().Where(x=>x.Id==id).Select(x=>new{x.IsActive,x.EmailConfirmed,x.SecurityStamp}).SingleOrDefaultAsync(ct);
        if(account is null||!account.IsActive||!account.EmailConfirmed||string.IsNullOrEmpty(account.SecurityStamp)||
            !string.Equals(account.SecurityStamp,context.Principal?.FindFirstValue(claims.SecurityStampClaimType),StringComparison.Ordinal))
        {await Reject(context);return;}
        var roles=await (from link in db.UserRoles.AsNoTracking() join role in db.Roles.AsNoTracking() on link.RoleId equals role.Id where link.UserId==id select role.Name!).ToArrayAsync(ct);
        var cookieRoles=context.Principal!.FindAll(claims.RoleClaimType).Select(x=>x.Value).ToHashSet(StringComparer.Ordinal);
        if(!cookieRoles.SetEquals(roles)){await Reject(context);return;}
        // Retain Identity's stamp renewal policy in addition to our per-request saved checks.
        await SecurityStampValidator.ValidatePrincipalAsync(context);
    }
    private static async Task Reject(CookieValidatePrincipalContext context)
    {context.RejectPrincipal();await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);}
}
