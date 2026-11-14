namespace BrokerHub.Infrastructure.Workers;

public sealed class NotificationConsumerOptions
{
    public const string SectionName="NotificationConsumer";
    public bool Enabled { get; set; }
    public int PrefetchCount { get; set; }=1;
    public int ReconnectSeconds { get; set; }=3;
    public int FailureDelaySeconds { get; set; }=1;
    public int ProcessingTimeoutSeconds { get; set; }=60;
    public int BrokerOperationTimeoutSeconds { get; set; }=5;
    public bool IsValid=>PrefetchCount is >=1 and <=32&&ReconnectSeconds is >=1 and <=60&&
        FailureDelaySeconds is >=1 and <=30&&ProcessingTimeoutSeconds is >=5 and <=180&&BrokerOperationTimeoutSeconds is >=1 and <=30;
}
