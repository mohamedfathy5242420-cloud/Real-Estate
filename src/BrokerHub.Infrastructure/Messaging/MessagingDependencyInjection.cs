using BrokerHub.Application.Interfaces.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using BrokerHub.Infrastructure.Workers;
using BrokerHub.Infrastructure.Storage;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Interfaces.Storage;

namespace BrokerHub.Infrastructure.Messaging;

internal static class MessagingDependencyInjection
{
    internal static IServiceCollection AddMessaging(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(o=>!o.Enabled||o.IsValid,"RabbitMQ configuration is incomplete or outside supported bounds.").ValidateOnStart();
        services.TryAddSingleton<IOutboxPublisher, RabbitMqOutboxPublisher>();
        services.AddScoped<INotificationMessageConsumer, NotificationMessageConsumer>();
        services.AddOptions<NotificationConsumerOptions>().Bind(configuration.GetSection(NotificationConsumerOptions.SectionName))
            .Validate(o=>o.IsValid,"Notification consumer settings are outside supported bounds.")
            .Validate<IOptions<RabbitMqOptions>>((o,r)=>!o.Enabled||r.Value.Enabled&&r.Value.IsValid,"Enabled notification consumer requires configured RabbitMQ.")
            .ValidateOnStart();
        services.AddHostedService<NotificationConsumerWorker>();
        services.AddOptions<LocalNotificationCaptureOptions>().Bind(configuration.GetSection(LocalNotificationCaptureOptions.SectionName))
            .Validate(o=>o.IsValid,"Owned local capture settings are outside supported bounds.").ValidateOnStart();
        services.AddSingleton<ILocalNotificationArtifactStore, LocalNotificationArtifactStore>();
        services.AddScoped<ILocalNotificationCapture, LocalNotificationCapture>();
        return services;
    }
}
