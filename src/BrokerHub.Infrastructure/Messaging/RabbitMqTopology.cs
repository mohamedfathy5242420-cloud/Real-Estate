using RabbitMQ.Client;

namespace BrokerHub.Infrastructure.Messaging;

internal static class RabbitMqTopology
{
    internal static async Task EnsureAsync(IChannel c,RabbitMqOptions o,CancellationToken ct)
    {
        await c.ExchangeDeclareAsync(o.EventExchange,ExchangeType.Direct,durable:true,autoDelete:false,cancellationToken:ct);
        await c.ExchangeDeclareAsync(o.DeadExchange,ExchangeType.Direct,durable:true,autoDelete:false,cancellationToken:ct);
        await c.QueueDeclareAsync(o.QuarantineQueue,durable:true,exclusive:false,autoDelete:false,arguments:RabbitMqConnectionSettings.QuarantineArguments(o),cancellationToken:ct);
        await c.QueueBindAsync(o.QuarantineQueue,o.DeadExchange,RabbitMqOptions.DeadRoutingKey,cancellationToken:ct);
        await c.QueueDeclareAsync(o.WorkQueue,durable:true,exclusive:false,autoDelete:false,arguments:RabbitMqConnectionSettings.WorkArguments(o),cancellationToken:ct);
        await c.QueueBindAsync(o.WorkQueue,o.EventExchange,RabbitMqOptions.EventRoutingKey,cancellationToken:ct);
        // Fresh quorum directory metadata has a documented early sync window. Stabilize BEFORE
        // any business send; cancellation here retains the SQL lease/source, never a false success.
        await Task.Delay(TimeSpan.FromSeconds(1),ct);
    }
}
