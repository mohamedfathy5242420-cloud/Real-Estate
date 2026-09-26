using BrokerHub.Application.Authorization;

namespace BrokerHub.Api.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddBrokerHubAuthorization(this IServiceCollection services)
    {
        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                AuthorizationPolicies.Customer,
                policy => policy.RequireAuthenticatedUser().RequireRole(AppRoles.Customer))
            .AddPolicy(
                AuthorizationPolicies.SalesEmployee,
                policy => policy.RequireAuthenticatedUser().RequireRole(AppRoles.SalesEmployee))
            .AddPolicy(
                AuthorizationPolicies.SalesManager,
                policy => policy.RequireAuthenticatedUser().RequireRole(AppRoles.SalesManager))
            .AddPolicy(
                AuthorizationPolicies.SalesStaff,
                policy => policy.RequireAuthenticatedUser().RequireRole(
                    AppRoles.SalesEmployee,
                    AppRoles.SalesManager))
            .AddPolicy(
                AuthorizationPolicies.SystemAdministrator,
                policy => policy.RequireAuthenticatedUser().RequireRole(
                    AppRoles.SystemAdministrator));

        return services;
    }
}
