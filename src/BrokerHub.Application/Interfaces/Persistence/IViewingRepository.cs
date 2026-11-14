using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;
public interface IViewingRepository
{
    void Add(Viewing viewing);
    void AddEvent(ViewingEvent history);
}
