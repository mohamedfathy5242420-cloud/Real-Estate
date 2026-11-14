using BrokerHub.Application.DTOs.Viewings;

namespace BrokerHub.Application.Interfaces.Persistence;
public interface IViewingCustomerQueries
{
    Task<ViewingResult<ViewingPage<CustomerViewingDto>>> MineAsync(Guid actor, int page, int size, CancellationToken ct);
    Task<ViewingResult<CustomerViewingDto>> GetAsync(Guid actor, Guid id, CancellationToken ct);
    Task<ViewingResult<ViewingPage<CustomerViewingEventDto>>> HistoryAsync(Guid actor, Guid id, int page, int size, CancellationToken ct);
    Task<ViewingResult<CustomerViewingEventDto>> EventAsync(Guid actor, Guid id, Guid eventId, CancellationToken ct);
}
