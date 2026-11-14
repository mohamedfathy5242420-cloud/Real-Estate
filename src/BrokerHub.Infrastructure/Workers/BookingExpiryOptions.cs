namespace BrokerHub.Infrastructure.Workers;
public sealed class BookingExpiryOptions
{
    public const string SectionName = "BookingExpiry";
    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 100;
    public int IntervalSeconds { get; set; } = 30;
}
