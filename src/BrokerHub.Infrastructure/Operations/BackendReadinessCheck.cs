using BrokerHub.Application.Interfaces.Storage;
using BrokerHub.Infrastructure.Identity;
using BrokerHub.Infrastructure.Messaging;
using BrokerHub.Infrastructure.Persistence;
using BrokerHub.Infrastructure.Storage;
using BrokerHub.Infrastructure.Workers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrokerHub.Infrastructure.Operations;

public sealed class BackendReadinessCheck(IServiceScopeFactory scopes,IConfiguration configuration,
    IHostEnvironment environment,IHttpContextAccessor http,ILogger<BackendReadinessCheck> logger):IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,CancellationToken ct=default)
    {
        var component="database";
        try
        {
            await using var scope=scopes.CreateAsyncScope();var services=scope.ServiceProvider;
            var db=services.GetRequiredService<ApplicationDbContext>();db.Database.SetCommandTimeout(TimeSpan.FromSeconds(3));
            if(!await db.Database.CanConnectAsync(ct))return Failed(component,"Unavailable");
            if((await db.Database.GetPendingMigrationsAsync(ct)).Any())return Failed(component,"PendingMigrations");
            component="local-profile";
            if(!environment.IsDevelopment())return Failed(component,"UnsupportedEnvironment");
            component="images";
            if(services.GetRequiredService<IUnitImageStorage>() is not LocalUnitImageStorage local||!await local.ProbeReadinessAsync(ct))return Failed(component,"Unavailable");
            component="keyring";var keys=configuration["DataProtection:KeysPath"];
            if(string.IsNullOrWhiteSpace(keys)||!OutsideContent(keys)||!await PrivateDirectoryProbe.WritableAsync(keys,ct))return Failed(component,"Unavailable");
            component="identity-capture";var capture=services.GetRequiredService<IOptions<DevelopmentIdentityCaptureOptions>>().Value;
            // Do not create probe entries in the bounded capture directory or consume a slot.
            if(!capture.Enabled||!capture.IsValid||!OutsideContent(capture.RootPath)||!PrivateDirectoryProbe.ExistsWithoutLinks(capture.RootPath)||
                Directory.EnumerateFileSystemEntries(capture.RootPath).Take(capture.MaximumRecords).Count()>=capture.MaximumRecords)return Failed(component,"Unavailable");
            component="messaging";var rabbit=services.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            var dispatch=services.GetRequiredService<IOptions<OutboxDispatchOptions>>().Value;
            var consumer=services.GetRequiredService<IOptions<NotificationConsumerOptions>>().Value;
            if(rabbit.Enabled||dispatch.Enabled||consumer.Enabled)
            {
                if(!rabbit.Enabled||!rabbit.IsValid)return Failed(component,"Unconfigured");
                var factory=RabbitMqConnectionSettings.Factory(rabbit);factory.ClientProvidedName="BrokerHub.Readiness.v1";
                await using var connection=await factory.CreateConnectionAsync(ct);
                await using var channel=await connection.CreateChannelAsync(cancellationToken:ct);
                await channel.ExchangeDeclarePassiveAsync(rabbit.EventExchange,ct);
                await channel.ExchangeDeclarePassiveAsync(rabbit.DeadExchange,ct);
                await channel.QueueDeclarePassiveAsync(rabbit.WorkQueue,ct);
                await channel.QueueDeclarePassiveAsync(rabbit.QuarantineQueue,ct);
            }
            return HealthCheckResult.Healthy();
        }
        catch(Exception ex){return Failed(component,ex.GetType().Name);}
    }
    private bool OutsideContent(string path)
    {
        if(!Path.IsPathFullyQualified(path))return false;
        var full=Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));var content=Path.TrimEndingDirectorySeparator(Path.GetFullPath(environment.ContentRootPath));
        return !full.Equals(content,StringComparison.OrdinalIgnoreCase)&&!full.StartsWith(content+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
    }
    private HealthCheckResult Failed(string component,string kind)
    {
        logger.LogWarning("Readiness dependency {Component} failed. Correlation {CorrelationId}; type {FailureType}",component,http.HttpContext?.TraceIdentifier??"none",kind);
        // Never hand a private exception to the framework health logger or response writer.
        return HealthCheckResult.Unhealthy("A configured dependency is unavailable.");
    }
}
