using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BrokerHub.Api.Filters;

// Keep public failures generic; diagnostics contain only correlation and exception type.
public sealed class CatalogReadExceptionAttribute : ExceptionFilterAttribute, IResourceFilter
{
    // Resource filters run before model binding and automatic ApiController validation responses.
    public void OnResourceExecuting(ResourceExecutingContext context) =>
        context.HttpContext.Response.Headers.CacheControl = "no-store";
    public void OnResourceExecuted(ResourceExecutedContext context) { }

    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is OperationCanceledException && context.HttpContext.RequestAborted.IsCancellationRequested) return;
        context.HttpContext.RequestServices.GetRequiredService<ILogger<CatalogReadExceptionAttribute>>()
            .LogWarning("Public catalog read failed. Correlation {CorrelationId}; type {ExceptionType}",context.HttpContext.TraceIdentifier,context.Exception.GetType().Name);
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        context.Result = new ObjectResult(new ProblemDetails { Status = 503, Title = "Catalog read is unavailable." }) { StatusCode = 503 };
        context.ExceptionHandled = true;
    }
}
