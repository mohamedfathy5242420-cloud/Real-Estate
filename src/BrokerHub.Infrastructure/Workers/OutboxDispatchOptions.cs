using BrokerHub.Application.DTOs.Messaging;

namespace BrokerHub.Infrastructure.Workers;

public sealed class OutboxDispatchOptions
{
    public const string SectionName="OutboxDispatch";
    public bool Enabled { get; set; }
    public int BatchSize { get; set; }=20;
    public int IntervalSeconds { get; set; }=5;
    public int LeaseSeconds { get; set; }=180;
    public int MaxAttempts { get; set; }=5;
    public int RetryBaseSeconds { get; set; }=5;
    public int RetryMaxSeconds { get; set; }=300;
    public OutboxDispatchPolicy Policy()=>new(BatchSize,LeaseSeconds,MaxAttempts,RetryBaseSeconds,RetryMaxSeconds);
}
