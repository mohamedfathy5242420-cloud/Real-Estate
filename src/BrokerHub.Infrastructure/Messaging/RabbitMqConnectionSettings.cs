using System.Net.Security;
using RabbitMQ.Client;

namespace BrokerHub.Infrastructure.Messaging;

internal static class RabbitMqConnectionSettings
{
    internal static ConnectionFactory Factory(RabbitMqOptions o)=>new()
    {
        HostName=o.HostName,Port=o.Port,VirtualHost=o.VirtualHost,UserName=o.UserName,Password=o.Password,
        AutomaticRecoveryEnabled=false,TopologyRecoveryEnabled=false,
        RequestedConnectionTimeout=TimeSpan.FromSeconds(o.ConnectTimeoutSeconds),
        HandshakeContinuationTimeout=TimeSpan.FromSeconds(o.ConnectTimeoutSeconds),
        ContinuationTimeout=TimeSpan.FromSeconds(o.PublishTimeoutSeconds),RequestedHeartbeat=TimeSpan.FromSeconds(o.HeartbeatSeconds),
        ClientProvidedName="BrokerHub.Outbox.v1",
        Ssl=new SslOption{Enabled=o.UseTls,ServerName=o.HostName,AcceptablePolicyErrors=SslPolicyErrors.None}
    };
    internal static Dictionary<string,object?> WorkArguments(RabbitMqOptions o)=>new()
    {
        ["x-queue-type"]="quorum",["x-overflow"]="reject-publish",["x-max-length"]=o.MaxQueueLength,["x-max-length-bytes"]=o.MaxQueueBytes,
        ["x-dead-letter-exchange"]=o.DeadExchange,["x-dead-letter-routing-key"]=RabbitMqOptions.DeadRoutingKey,
        ["x-dead-letter-strategy"]="at-least-once",["x-delivery-limit"]=o.DeliveryLimit
    };
    internal static Dictionary<string,object?> QuarantineArguments(RabbitMqOptions o)=>new()
    {
        ["x-queue-type"]="quorum",["x-overflow"]="reject-publish",["x-max-length"]=o.MaxQueueLength,["x-max-length-bytes"]=o.MaxQueueBytes
    };
}
