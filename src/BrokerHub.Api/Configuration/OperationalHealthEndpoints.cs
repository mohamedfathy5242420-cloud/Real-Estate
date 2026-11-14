using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BrokerHub.Api.Configuration;

public static class OperationalHealthEndpoints
{
    public static void MapBrokerHubHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/healthz",new HealthCheckOptions{Predicate=_=>false,ResponseWriter=(ctx,_)=>Write(ctx,"alive")})
            .WithMetadata(new HttpMethodMetadata(["GET","HEAD"]));
        app.MapHealthChecks("/readyz",new HealthCheckOptions{Predicate=entry=>entry.Tags.Contains("ready"),ResponseWriter=(ctx,report)=>Write(ctx,report.Status==HealthStatus.Healthy?"ready":"not-ready")})
            .WithMetadata(new HttpMethodMetadata(["GET","HEAD"]));
    }
    private static Task Write(HttpContext context,string status)
    {
        context.Response.ContentType="application/json";
        return HttpMethods.IsHead(context.Request.Method)?Task.CompletedTask:context.Response.WriteAsJsonAsync(new{status},cancellationToken:context.RequestAborted);
    }
}
