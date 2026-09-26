using Microsoft.Extensions.DependencyInjection;

namespace BrokerHub.Infrastructure.Time;

internal static class TimeDependencyInjection
{
    internal static IServiceCollection AddTimeServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
