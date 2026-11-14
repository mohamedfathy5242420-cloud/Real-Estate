using Microsoft.AspNetCore.Http;

namespace BrokerHub.Api.Configuration;

internal static class NotificationApiSecurity
{
    internal const string HeaderName="X-CSRF-TOKEN";
    internal static IServiceCollection AddNotificationApiSecurity(this IServiceCollection services,IHostEnvironment environment)
    {
        services.AddAntiforgery(o=>
        {
            o.HeaderName=HeaderName;o.Cookie.Name=environment.IsDevelopment()?"BrokerHub.NotificationCsrf.Development":"__Host-BrokerHub.NotificationCsrf";
            o.SuppressReadingTokenFromFormBody=true;
            o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Strict;o.Cookie.Path="/";
            o.Cookie.SecurePolicy=environment.IsDevelopment()?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;
        });
        return services;
    }
}
