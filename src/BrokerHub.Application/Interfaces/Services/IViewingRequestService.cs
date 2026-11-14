using BrokerHub.Application.DTOs.Viewings;

namespace BrokerHub.Application.Interfaces.Services;
public interface IViewingRequestService
{
    Task<ViewingResult<ViewingReply>> SubmitAsync(SubmitViewingDto request, string key, CancellationToken ct);
    Task<ViewingResult<ViewingPage<CustomerViewingDto>>> MineAsync(int page, int size, CancellationToken ct);
    Task<ViewingResult<CustomerViewingDto>> GetAsync(Guid id, CancellationToken ct);
    Task<ViewingResult<ViewingPage<CustomerViewingEventDto>>> HistoryAsync(Guid id, int page, int size, CancellationToken ct);
    Task<ViewingResult<CustomerViewingEventDto>> EventAsync(Guid id, Guid eventId, CancellationToken ct);
}
