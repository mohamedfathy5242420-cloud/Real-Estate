namespace BrokerHub.Api.Middleware;

public sealed class NotificationNoStoreMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if(context.Request.Path.StartsWithSegments("/api/notifications"))context.Response.Headers.CacheControl="no-store";
        await next(context);
    }
}
