using System.Net;

namespace BrokerHub.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName="RabbitMq";
    public bool Enabled { get; set; }
    public string HostName { get; set; }="localhost";
    public int Port { get; set; }=5672;
    public string VirtualHost { get; set; }="/brokerhub";
    public string UserName { get; set; }="";
    public string Password { get; set; }="";
    public bool UseTls { get; set; }
    public string NamePrefix { get; set; }="brokerhub";
    public int PublishTimeoutSeconds { get; set; }=5;
    public int ConnectTimeoutSeconds { get; set; }=3;
    public int HeartbeatSeconds { get; set; }=15;
    public int MaxQueueLength { get; set; }=10000;
    public int MaxQueueBytes { get; set; }=64*1024*1024;
    public int DeliveryLimit { get; set; }=5;
    public string EventExchange=>NamePrefix+".events.v1";
    public string WorkQueue=>NamePrefix+".notifications.v1";
    public string DeadExchange=>NamePrefix+".dead.v1";
    public string QuarantineQueue=>NamePrefix+".quarantine.v1";
    public const string EventRoutingKey="events.v1",DeadRoutingKey="invalid.v1";
    public bool IsValid=>!string.IsNullOrWhiteSpace(HostName)&&HostName.Length<=253&&Uri.CheckHostName(HostName)!=UriHostNameType.Unknown&&
        (UseTls||HostName.Equals("localhost",StringComparison.OrdinalIgnoreCase)||IPAddress.TryParse(HostName,out var ip)&&IPAddress.IsLoopback(ip))&&
        Port is >=1 and <=65535&&!string.IsNullOrWhiteSpace(VirtualHost)&&VirtualHost.Length<=128&&
        !string.IsNullOrWhiteSpace(UserName)&&UserName.Length<=128&&!string.IsNullOrEmpty(Password)&&Password.Length<=1024&&
        !string.IsNullOrEmpty(NamePrefix)&&NamePrefix.Length<=64&&char.IsAsciiLetter(NamePrefix[0])&&
        NamePrefix.All(c=>char.IsAsciiLetterOrDigit(c)||c is '.' or '_' or '-')&&!NamePrefix.StartsWith("amq.",StringComparison.OrdinalIgnoreCase)&&
        PublishTimeoutSeconds is >=1 and <=30&&ConnectTimeoutSeconds is >=1 and <=30&&ConnectTimeoutSeconds<=PublishTimeoutSeconds&&
        HeartbeatSeconds is >=5 and <=300&&MaxQueueLength is >=1 and <=1000000&&MaxQueueBytes is >=131072 and <=1073741824&&DeliveryLimit is >=1 and <=20;
}
