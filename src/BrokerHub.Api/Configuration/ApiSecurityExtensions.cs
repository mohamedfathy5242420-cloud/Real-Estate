using System.Text.Json.Serialization;
using BrokerHub.Api.Serialization;

namespace BrokerHub.Api.Configuration;

public static class ApiSecurityExtensions
{
    public const string CorsPolicy="BrokerHub.Frontend";
    public static IServiceCollection AddBrokerHubApiSecurity(this IServiceCollection services,IConfiguration configuration,IHostEnvironment environment)
    {
        var settings=configuration.GetSection(ApiSecurityOptions.SectionName).Get<ApiSecurityOptions>()??new();
        if(!settings.IsValid(environment.IsDevelopment()))throw new InvalidOperationException("An explicit valid Frontend origin allowlist is required.");
        services.AddSingleton(settings);
        services.AddCors(o=>o.AddPolicy(CorsPolicy,p=>p.WithOrigins(settings.FrontendOrigins).AllowCredentials()
            .WithMethods("GET","POST","PUT","PATCH","DELETE","OPTIONS").WithHeaders("Content-Type","X-CSRF-TOKEN","Idempotency-Key")
            .WithExposedHeaders("Location","X-Correlation-ID")));
        services.AddNotificationApiSecurity(environment);
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o=>
        {
            o.JsonSerializerOptions.MaxDepth=32;o.JsonSerializerOptions.UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow;
            o.JsonSerializerOptions.Converters.Add(new LosslessDecimalConverter());o.JsonSerializerOptions.Converters.Add(new LosslessInt64Converter());
            o.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());o.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());
        });
        services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(o=>o.Limits.MaxRequestBodySize=ApiSecurityOptions.MaximumApiBytes);
        return services;
    }
}
