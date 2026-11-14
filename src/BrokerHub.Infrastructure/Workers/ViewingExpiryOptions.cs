namespace BrokerHub.Infrastructure.Workers;
public sealed class ViewingExpiryOptions
{
    public const string SectionName = "ViewingExpiry";
    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 100;
    public int IntervalSeconds { get; set; } = 30;
}
