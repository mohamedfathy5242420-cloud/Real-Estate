using Microsoft.Extensions.DependencyInjection;

namespace BrokerHub.Infrastructure.Caching;

internal static class CachingDependencyInjection
{
    internal static IServiceCollection AddCaching(this IServiceCollection services)
    {
        return services;
    }
}
