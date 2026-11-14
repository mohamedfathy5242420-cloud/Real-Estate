using BrokerHub.Application.DTOs.Messaging;
using BrokerHub.Application.Interfaces.Messaging;
using BrokerHub.Domain.Enums;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace BrokerHub.Infrastructure.Messaging;

internal sealed class RabbitMqOutboxPublisher(IOptions<RabbitMqOptions> options):IOutboxPublisher,IAsyncDisposable
{
    private readonly SemaphoreSlim gate=new(1,1);
    private IConnection? connection;
    private IChannel? channel;
    private bool disposed;
    public bool IsConfigured=>!disposed&&options.Value.Enabled&&options.Value.IsValid;
    public async Task<OutboxPublishResult> PublishAsync(OutboxEnvelope envelope,CancellationToken ct)
    {
        if(!IsConfigured)throw new InvalidOperationException("RabbitMQ publisher is not configured.");
        byte[] body;try{body=OutboxEnvelopeWire.Serialize(envelope);}catch(ArgumentException){return new(OutboxPublishDisposition.PermanentFailure,OutboxFailureKind.InvalidEnvelope);}
        var settings=options.Value;using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(settings.PublishTimeoutSeconds));var entered=false;var attempted=false;
        try
        {
            await gate.WaitAsync(deadline.Token);entered=true;
            if(disposed)throw new ObjectDisposedException(nameof(RabbitMqOutboxPublisher));
            if(connection?.IsOpen!=true||channel?.IsOpen!=true)
            {
                await Reset();connection=await RabbitMqConnectionSettings.Factory(settings).CreateConnectionAsync(deadline.Token);
                channel=await connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled:true,publisherConfirmationTrackingEnabled:true),deadline.Token);
                await RabbitMqTopology.EnsureAsync(channel,settings,deadline.Token);
            }
            var props=new BasicProperties{MessageId=envelope.MessageId.ToString("N"),ContentType="application/json",Type=envelope.Name,
                DeliveryMode=DeliveryModes.Persistent,AppId="BrokerHub.Outbox.v1"};
            attempted=true;
            // With both channel options enabled this await includes broker confirmation/return tracking.
            await channel.BasicPublishAsync(settings.EventExchange,RabbitMqOptions.EventRoutingKey,mandatory:true,props,body,deadline.Token);
            return new(OutboxPublishDisposition.Confirmed);
        }
        catch(PublishException ex)
        {
            if(entered)await Reset();return ex.IsReturn?new(OutboxPublishDisposition.PermanentFailure,OutboxFailureKind.Rejected):
                new(OutboxPublishDisposition.RetryableFailure,OutboxFailureKind.NotConfirmed);
        }
        catch(OperationCanceledException) when(ct.IsCancellationRequested){if(entered)await Reset();throw;}
        catch(AuthenticationFailureException){if(entered)await Reset();return new(OutboxPublishDisposition.PermanentFailure,OutboxFailureKind.Rejected);}
        catch(Exception)
        {
            if(entered)await Reset();return attempted?new(OutboxPublishDisposition.Uncertain,OutboxFailureKind.ConfirmUncertain):
                new(OutboxPublishDisposition.RetryableFailure,OutboxFailureKind.TransportUnavailable);
        }
        finally{if(entered)gate.Release();}
    }
    private async Task Reset()
    {
        var oldChannel=channel;var oldConnection=connection;channel=null;connection=null;
        if(oldChannel is not null)try{await oldChannel.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));}catch(Exception){}
        if(oldConnection is not null)try{await oldConnection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));}catch(Exception){}
    }
    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();try{if(disposed)return;disposed=true;await Reset();}finally{gate.Release();}
    }
}
