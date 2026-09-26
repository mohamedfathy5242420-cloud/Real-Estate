using Microsoft.Extensions.DependencyInjection;

namespace BrokerHub.Infrastructure.Messaging;

internal static class MessagingDependencyInjection
{
    internal static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        return services;
    }
}
