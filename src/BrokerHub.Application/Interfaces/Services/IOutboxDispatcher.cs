using BrokerHub.Application.DTOs.Messaging;

namespace BrokerHub.Application.Interfaces.Services;

public interface IOutboxDispatcher
{
    Task<OutboxDispatchResult> DispatchBatchAsync(OutboxDispatchPolicy policy,CancellationToken ct);
}
