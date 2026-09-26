using BrokerHub.Infrastructure.Caching;
using BrokerHub.Infrastructure.Identity;
using BrokerHub.Infrastructure.Messaging;
using BrokerHub.Infrastructure.Persistence;
using BrokerHub.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BrokerHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddPersistence(configuration)
            .AddIdentityInfrastructure()
            .AddMessaging()
            .AddCaching()
            .AddTimeServices();

        return services;
    }
}
