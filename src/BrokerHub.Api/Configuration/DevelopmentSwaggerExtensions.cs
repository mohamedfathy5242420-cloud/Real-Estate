using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BrokerHub.Api.Configuration;

/// <summary>Local API explorer only; never changes application authorization or CSRF rules.</summary>
public static class DevelopmentSwaggerExtensions
{
    public static IServiceCollection AddDevelopmentSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BrokerHub API",
                Version = "v1",
                Description = "Development explorer. Sign in using POST /api/accounts/login with an existing " +
                    "confirmed account; same-origin session cookies are used, not a Bearer token. " +
                    "Try it out obtains a fresh CSRF token automatically before unsafe API calls. " +
                    "Protected operations still require the correct role and current ownership/assignment. " +
                    "Use UTC timestamps with Z, and strings for exact money and Int64 versions. " +
                    "Database and other configured dependencies must be ready for business operations."
            });
            options.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace('+', '.'));
            options.MapType<decimal>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Description = "Exact fixed-point decimal; the API serializes money as strings.",
                Example = JsonValue.Create("100.00")
            });
            options.MapType<long>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Description = "Exact Int64 integer; versions are serialized as strings.",
                Example = JsonValue.Create("1")
            });
            options.OperationFilter<DevelopmentSwaggerOperationFilter>();
        });
        return services;
    }

    public static IApplicationBuilder UseDevelopmentSwagger(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/" &&
                (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)))
            {
                context.Response.Redirect(context.Request.PathBase + "/swagger/index.html");
                return;
            }
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.CacheControl = "no-store";
                    context.Response.Headers.XContentTypeOptions = "nosniff";
                    return Task.CompletedTask;
                });
            }
            await next(context);
        });
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "BrokerHub — Swagger";
            options.SwaggerEndpoint("v1/swagger.json", "BrokerHub API v1");
            options.EnableFilter();
            options.DisplayRequestDuration();
            options.ConfigObject.AdditionalItems["withCredentials"] = true;
            options.UseRequestInterceptor("""
                (request) => {
                    const target = new URL(request.url, window.location.href);
                    if (target.origin !== window.location.origin) {
                        throw new Error("BrokerHub Swagger only sends requests to its own origin.");
                    }
                    request.credentials = "same-origin";
                    const method = (request.method || "GET").toUpperCase();
                    if (!["GET", "HEAD", "OPTIONS", "TRACE"].includes(method) && /\/api(?:\/|$)/.test(target.pathname)) {
                        return window.fetch(new URL("../api/session/antiforgery", window.location.href), {
                            credentials: "same-origin", cache: "no-store", headers: { "Accept": "application/json" }
                        }).then(response => {
                            if (!response.ok) throw new Error("Could not obtain the session CSRF token.");
                            return response.json();
                        }).then(session => {
                            if (!session.token || session.headerName !== "X-CSRF-TOKEN") {
                                throw new Error("Invalid session CSRF response.");
                            }
                            request.headers = request.headers || {};
                            request.headers[session.headerName] = session.token;
                            return request;
                        });
                    }
                    return request;
                }
                """);
        });
        return app;
    }
}

internal sealed class DevelopmentSwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters is null) return;
        foreach (var parameter in operation.Parameters)
        {
            if (parameter is OpenApiParameter header &&
                header.In == ParameterLocation.Header && header.Name == "Idempotency-Key")
            {
                header.Required = true;
                header.Description = "New unique key for a new intentional command. Reuse the same key " +
                    "only when retrying the same payload; changing the payload requires a new key.";
            }
        }
    }
}
