using BrokerHub.Domain.Entities;
using BrokerHub.Application.DTOs.Deals;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IDealRepository
{
    void Add(Deal deal);
    void Add(DealBookingLink link);
    Task<Deal?> LockAsync(Guid customerId,Guid id,CancellationToken ct);
    Task<CommissionCollectionLedger> CollectionLedgerAsync(Guid dealId,CancellationToken ct);
    void Add(CommissionCollection collection);
}
