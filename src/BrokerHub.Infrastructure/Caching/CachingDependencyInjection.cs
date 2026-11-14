using BrokerHub.Application.Interfaces.Storage;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Infrastructure.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BrokerHub.Infrastructure.Caching;

internal static class CachingDependencyInjection
{
    internal static IServiceCollection AddCaching(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddOptions<PublicImageCacheOptions>().Bind(configuration.GetSection(PublicImageCacheOptions.SectionName))
            .Validate(o=>o.IsValid,"Public image cache settings are outside supported bounds.").ValidateOnStart();
        services.AddSingleton<CachedPublicUnitImageReader>();
        services.AddSingleton<IPublicUnitImageReader>(provider=>provider.GetRequiredService<CachedPublicUnitImageReader>());
        services.AddOptions<PublicImageWarmupOptions>().Bind(configuration.GetSection(PublicImageWarmupOptions.SectionName))
            .Validate(o=>o.IsValid,"Public image warmup settings are outside supported bounds.").ValidateOnStart();
        services.AddSingleton<PublicImageWarmupWorker>();
        services.AddSingleton<IPublicImageWarmupQueue>(provider=>provider.GetRequiredService<PublicImageWarmupWorker>());
        services.AddHostedService(provider=>provider.GetRequiredService<PublicImageWarmupWorker>());
        return services;
    }
}
