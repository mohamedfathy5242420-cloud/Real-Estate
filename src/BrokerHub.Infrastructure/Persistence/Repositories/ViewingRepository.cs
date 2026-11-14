using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Infrastructure.Persistence.Repositories;
internal sealed class ViewingRepository(ApplicationDbContext db) : IViewingRepository
{
    public void Add(Viewing viewing) { RequireTransaction(); db.Viewings.Add(viewing); }
    public void AddEvent(ViewingEvent history) { RequireTransaction(); db.ViewingEvents.Add(history); }
    private void RequireTransaction()
    { if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Viewing staging requires the command transaction."); }
}
