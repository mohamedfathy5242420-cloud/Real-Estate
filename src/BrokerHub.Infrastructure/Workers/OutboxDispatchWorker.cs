using BrokerHub.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrokerHub.Infrastructure.Workers;

internal sealed class OutboxDispatchWorker(IServiceScopeFactory scopes,IOptions<OutboxDispatchOptions> options,
    TimeProvider clock,ILogger<OutboxDispatchWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings=options.Value;if(!settings.Enabled)return;var warned=false;
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope=scopes.CreateAsyncScope();
                var result=await scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>().DispatchBatchAsync(settings.Policy(),stoppingToken);
                if(!result.PublisherConfigured&&!warned){logger.LogWarning("Outbox dispatcher has no configured publisher; durable events remain unclaimed.");warned=true;}
                if(result.Published>0||result.DeadLettered>0)logger.LogInformation("Outbox batch published {Published}, quarantined {DeadLettered}, lease lost {LeaseLost}.",result.Published,result.DeadLettered,result.LeaseLost);
                if(result.Inspected==settings.BatchSize&&result.Published+result.DeadLettered>0)continue;
            }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogWarning("Outbox dispatch batch failed ({ExceptionType}); durable state will be retried.",ex.GetType().Name);}
            try{await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds),clock,stoppingToken);}
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
        }
    }
}
