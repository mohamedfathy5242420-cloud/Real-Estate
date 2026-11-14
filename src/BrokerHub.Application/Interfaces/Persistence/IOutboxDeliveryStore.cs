using BrokerHub.Application.DTOs.Messaging;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IOutboxDeliveryStore
{
    Task<OutboxClaimBatch> ClaimAsync(OutboxDispatchPolicy policy,CancellationToken ct);
    Task<OutboxCompletionStatus> CompleteAsync(Guid deliveryId,Guid leaseToken,OutboxPublishResult result,OutboxDispatchPolicy policy,CancellationToken ct);
}
