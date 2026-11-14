using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Middleware;

public sealed class ApiResponseBoundaryMiddleware(RequestDelegate next,ILogger<ApiResponseBoundaryMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path=context.Request.Path;
        if(!path.StartsWithSegments("/api")&&!path.Equals("/healthz",StringComparison.OrdinalIgnoreCase)&&!path.Equals("/readyz",StringComparison.OrdinalIgnoreCase)){await next(context);return;}
        var correlation=Guid.NewGuid().ToString("N");context.TraceIdentifier=correlation;
        void Headers(){context.Response.Headers.CacheControl="no-store";context.Response.Headers.XContentTypeOptions="nosniff";context.Response.Headers["X-Correlation-ID"]=correlation;context.Response.Headers["Referrer-Policy"]="no-referrer";}
        Headers();context.Response.OnStarting(()=>{Headers();return Task.CompletedTask;});
        try{await next(context);}
        catch(OperationCanceledException)when(context.RequestAborted.IsCancellationRequested){context.Abort();}
        catch(Exception ex)when(!context.Response.HasStarted)
        {
            var status=ex is BadHttpRequestException bad?bad.StatusCode:503;
            if(status is not (400 or 413 or 415))status=503;
            logger.LogWarning("API request failed. Correlation {CorrelationId}; type {ExceptionType}; status {StatusCode}",correlation,ex.GetType().Name,status);
            context.Response.Clear();Headers();context.Response.StatusCode=status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails{Status=status,Title=status==413?"The request exceeds the allowed size.":"The API operation is unavailable.",Extensions={ ["correlationId"]=correlation }},options:(System.Text.Json.JsonSerializerOptions?)null,contentType:"application/problem+json",cancellationToken:context.RequestAborted);
        }
        catch(Exception ex)
        {
            logger.LogWarning("API stream aborted. Correlation {CorrelationId}; type {ExceptionType}",correlation,ex.GetType().Name);
            context.Abort();
        }
    }
}
