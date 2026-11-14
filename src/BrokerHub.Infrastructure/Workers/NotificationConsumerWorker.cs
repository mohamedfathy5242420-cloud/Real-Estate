using BrokerHub.Application.DTOs.Messaging;
using BrokerHub.Application.Interfaces.Messaging;
using BrokerHub.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BrokerHub.Infrastructure.Workers;

internal sealed class NotificationConsumerWorker(IServiceScopeFactory scopes,IOptions<NotificationConsumerOptions> options,
    IOptions<RabbitMqOptions> rabbit,ILogger<NotificationConsumerWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(!options.Value.Enabled)return;
        while(!stoppingToken.IsCancellationRequested)
        {
            try{await Session(stoppingToken);}
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogWarning("Notification consumer connection failed ({ExceptionType}); durable messages remain recoverable.",ex.GetType().Name);}
            try{await Task.Delay(TimeSpan.FromSeconds(options.Value.ReconnectSeconds),stoppingToken);}
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
        }
    }
    private async Task Session(CancellationToken stoppingToken)
    {
        var settings=options.Value;var factory=RabbitMqConnectionSettings.Factory(rabbit.Value);factory.ConsumerDispatchConcurrency=1;
        using var session=CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        using var startup=CancellationTokenSource.CreateLinkedTokenSource(session.Token);startup.CancelAfter(TimeSpan.FromSeconds(rabbit.Value.PublishTimeoutSeconds));
        await using var connection=await factory.CreateConnectionAsync(startup.Token);
        await using var channel=await connection.CreateChannelAsync(cancellationToken:startup.Token);
        var ended=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionShutdownAsync+=(_,_)=>{ended.TrySetResult();return Task.CompletedTask;};
        channel.ChannelShutdownAsync+=(_,_)=>{ended.TrySetResult();return Task.CompletedTask;};
        await RabbitMqTopology.EnsureAsync(channel,rabbit.Value,startup.Token);
        await channel.BasicQosAsync(0,(ushort)settings.PrefetchCount,global:false,startup.Token);
        Task active=Task.CompletedTask;var sync=new object();var consumer=new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync+=async(_,delivery)=>
        {
            if(session.IsCancellationRequested)return;
            // The library owns the receive buffer; no reference escapes this callback.
            var body=delivery.Body.Length<=OutboxEnvelopeWire.MaxBytes?delivery.Body.ToArray():Array.Empty<byte>();
            var properties=new NotificationWireProperties(delivery.BasicProperties.MessageId,delivery.BasicProperties.ContentType,
                delivery.BasicProperties.Type,delivery.BasicProperties.Persistent);
            var processing=Handle(channel,delivery.DeliveryTag,body,properties,session.Token,ended);
            lock(sync)active=processing;
            await processing;
        };
        await channel.BasicConsumeAsync(rabbit.Value.WorkQueue,autoAck:false,consumer,startup.Token);
        try{await ended.Task.WaitAsync(stoppingToken);}
        finally
        {
            session.Cancel();Task processing;lock(sync)processing=active;
            // Drain the current cancellable SQL scope before disposing channel/connection.
            await processing;
        }
    }
    private async Task Handle(IChannel channel,ulong tag,byte[] body,NotificationWireProperties properties,
        CancellationToken sessionToken,TaskCompletionSource ended)
    {
        try
        {
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(sessionToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(options.Value.ProcessingTimeoutSeconds));
            await using var scope=scopes.CreateAsyncScope();
            var consumer=scope.ServiceProvider.GetRequiredService<INotificationMessageConsumer>();
            var result=await consumer.ConsumeAsync(body,properties,deadline.Token);
            using var ack=CancellationTokenSource.CreateLinkedTokenSource(sessionToken);
            ack.CancelAfter(TimeSpan.FromSeconds(options.Value.BrokerOperationTimeoutSeconds));
            if(result.Status==NotificationConsumeStatus.Rejected)await channel.BasicRejectAsync(tag,requeue:false,ack.Token);
            else await channel.BasicAckAsync(tag,multiple:false,ack.Token);
        }
        catch(OperationCanceledException)when(sessionToken.IsCancellationRequested){ }
        catch(Exception ex)
        {
            logger.LogWarning("Notification handling failed ({ExceptionType}); broker failed-delivery retry remains bounded.",ex.GetType().Name);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(options.Value.FailureDelaySeconds),sessionToken);
                using var reject=CancellationTokenSource.CreateLinkedTokenSource(sessionToken);
                reject.CancelAfter(TimeSpan.FromSeconds(options.Value.BrokerOperationTimeoutSeconds));
                // Rabbit4.3 basic.nack would not increase the failed-delivery counter.
                await channel.BasicRejectAsync(tag,requeue:true,reject.Token);
            }
            catch(OperationCanceledException)when(sessionToken.IsCancellationRequested){ }
            catch(Exception){ended.TrySetResult();}
        }
    }
}
