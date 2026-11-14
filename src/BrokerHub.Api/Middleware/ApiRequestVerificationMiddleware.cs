using BrokerHub.Api.Configuration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace BrokerHub.Api.Middleware;

public sealed class ApiRequestVerificationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context,IAntiforgery antiforgery,ApiSecurityOptions settings)
    {
        var request=context.Request;
        if(!request.Path.StartsWithSegments("/api")){await next(context);return;}
        var maximum=request.HasFormContentType?ApiSecurityOptions.MaximumApiBytes:ApiSecurityOptions.MaximumJsonBytes;
        if(request.ContentLength>maximum){context.Response.StatusCode=413;return;}
        // A declared oversize body is rejected without lowering the drain limit on its connection.
        if(context.Features.Get<IHttpMaxRequestBodySizeFeature>() is {IsReadOnly:false} feature)feature.MaxRequestBodySize=maximum;
        if(!HttpMethods.IsGet(request.Method)&&!HttpMethods.IsHead(request.Method)&&!HttpMethods.IsOptions(request.Method)&&!HttpMethods.IsTrace(request.Method))
        {
            var origins=request.Headers.Origin;var sameOrigin=request.Scheme+"://"+request.Host;
            if(origins.Count>1||origins.Count==1&&(!string.Equals(origins[0],sameOrigin,StringComparison.OrdinalIgnoreCase)&&!settings.FrontendOrigins.Contains(origins[0],StringComparer.Ordinal)))
            {await Invalid(context,403,"The request origin is not permitted.");return;}
            try{await antiforgery.ValidateRequestAsync(context);}
            catch(AntiforgeryValidationException){await Invalid(context,400,"Request verification failed. Refresh the session token.");return;}
        }
        await next(context);
    }
    private static Task Invalid(HttpContext context,int status,string title)
    {
        context.Response.StatusCode=status;
        return context.Response.WriteAsJsonAsync(new ProblemDetails{Status=status,Title=title},options:(System.Text.Json.JsonSerializerOptions?)null,contentType:"application/problem+json",cancellationToken:context.RequestAborted);
    }
}
