namespace BrokerHub.Infrastructure.Workers;

public sealed class PublicImageWarmupOptions
{
    public const string SectionName="PublicImageWarmup";
    public bool Enabled { get; set; }
    public int Capacity { get; set; }=32;
    public int MaximumWaitingOffers { get; set; }=4;
    public int MaximumImagesPerOffer { get; set; }=3;
    public int ProducerWaitMilliseconds { get; set; }=10;
    public int ItemTimeoutSeconds { get; set; }=10;
    public bool IsValid=>Capacity is >=1 and <=128&&MaximumWaitingOffers is >=1 and <=8&&MaximumImagesPerOffer is >=1 and <=10&&
        ProducerWaitMilliseconds is >=1 and <=50&&ItemTimeoutSeconds is >=1 and <=30;
}
