using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using BrokerHub.Application.DTOs.Deals;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Repositories;

internal sealed class DealRepository(ApplicationDbContext db):IDealRepository
{
    public void Add(Deal deal){RequireTransaction();db.Deals.Add(deal);}
    public void Add(DealBookingLink link){RequireTransaction();db.DealBookingLinks.Add(link);}
    public void Add(CommissionCollection collection){RequireTransaction();db.CommissionCollections.Add(collection);}
    public Task<Deal?> LockAsync(Guid customerId,Guid id,CancellationToken ct)
    {
        RequireTransaction();
        return db.Deals.FromSqlInterpolated($"SELECT * FROM [Sales].[Deals] WITH (UPDLOCK,HOLDLOCK) WHERE [CustomerId]={customerId} AND [Id]={id}").SingleOrDefaultAsync(ct);
    }
    public async Task<CommissionCollectionLedger> CollectionLedgerAsync(Guid dealId,CancellationToken ct)
    {
        RequireTransaction();
        var rows=db.CommissionCollections.Where(x=>x.DealId==dealId);
        return new(await rows.SumAsync(x=>(decimal?)x.Amount,ct)??0m,await rows.MaxAsync(x=>(DateTime?)x.RecordedAtUtc,ct),await rows.CountAsync(ct));
    }
    private void RequireTransaction(){if(db.Database.CurrentTransaction is null)throw new InvalidOperationException("Deal staging requires the existing command transaction.");}
}
