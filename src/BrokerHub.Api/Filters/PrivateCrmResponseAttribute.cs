using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BrokerHub.Api.Filters;

public sealed class PrivateCrmResponseAttribute : Attribute, IResourceFilter, IExceptionFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context) => context.HttpContext.Response.Headers.CacheControl = "no-store";
    public void OnResourceExecuted(ResourceExecutedContext context) { }
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is OperationCanceledException && context.HttpContext.RequestAborted.IsCancellationRequested) return;
        context.HttpContext.RequestServices.GetRequiredService<ILogger<PrivateCrmResponseAttribute>>().LogWarning("Private CRM operation failed. Correlation {CorrelationId}; type {ExceptionType}",context.HttpContext.TraceIdentifier,context.Exception.GetType().Name);
        context.ExceptionHandled = true;
        context.Result = new ObjectResult(new ProblemDetails { Status = 503, Title = "The operation is temporarily unavailable." }) { StatusCode = 503 };
    }
}
