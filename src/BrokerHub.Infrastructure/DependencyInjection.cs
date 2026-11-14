using BrokerHub.Infrastructure.Caching;
using BrokerHub.Infrastructure.Identity;
using BrokerHub.Infrastructure.Messaging;
using BrokerHub.Infrastructure.Persistence;
using BrokerHub.Infrastructure.Time;
using BrokerHub.Infrastructure.Storage;
using BrokerHub.Infrastructure.Workers;
using BrokerHub.Application.Interfaces.Storage;
using BrokerHub.Infrastructure.Operations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BrokerHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services
            .AddPersistence(configuration)
            .AddIdentityInfrastructure(configuration, environment)
            .AddMessaging(configuration)
            .AddCaching(configuration)
            .AddTimeServices();

        services.AddOptions<UnitImageStorageOptions>()
            .Bind(configuration.GetSection(UnitImageStorageOptions.SectionName));
        services.AddSingleton(environment);
        services.AddSingleton<IUnitImageStorage, LocalUnitImageStorage>();
        services.AddHealthChecks().AddCheck<BackendReadinessCheck>("backend-ready",HealthStatus.Unhealthy,["ready"],TimeSpan.FromSeconds(8));
        services.AddOptions<ViewingExpiryOptions>().Bind(configuration.GetSection(ViewingExpiryOptions.SectionName))
            .Validate(o => o.BatchSize is >= 1 and <= 500 && o.IntervalSeconds is >= 1 and <= 3600, "Viewing expiry batch/interval outside supported bounds.")
            .ValidateOnStart();
        services.AddHostedService<ViewingExpiryWorker>();
        services.AddOptions<BookingExpiryOptions>().Bind(configuration.GetSection(BookingExpiryOptions.SectionName))
            .Validate(o => o.BatchSize is >= 1 and <= 500 && o.IntervalSeconds is >= 1 and <= 3600, "Booking expiry batch/interval outside supported bounds.")
            .ValidateOnStart();
        services.AddHostedService<BookingExpiryWorker>();
        services.AddOptions<OutboxDispatchOptions>().Bind(configuration.GetSection(OutboxDispatchOptions.SectionName))
            .Validate(o=>o.Policy().IsValid&&o.IntervalSeconds is >=1 and <=3600,"Outbox dispatch policy outside supported bounds.")
            .ValidateOnStart();
        services.AddHostedService<OutboxDispatchWorker>();
        return services;
    }
}
