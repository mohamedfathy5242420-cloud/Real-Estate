using BrokerHub.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrokerHub.Infrastructure.Workers;
internal sealed class ViewingExpiryWorker(IServiceScopeFactory scopes, IOptions<ViewingExpiryOptions> options,
    TimeProvider clock, ILogger<ViewingExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value; if (!settings.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var result = await scope.ServiceProvider.GetRequiredService<IViewingExpiryService>().ExpireDueAsync(settings.BatchSize, stoppingToken);
                if (result.Expired > 0) logger.LogInformation("Viewing expiry committed {Expired} transitions from {Inspected} candidates.", result.Expired, result.Inspected);
                if (result.Inspected == settings.BatchSize && result.Expired > 0) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning("Viewing expiry batch failed ({ExceptionType}); retrying after interval.", ex.GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), clock, stoppingToken);
        }
    }
}
