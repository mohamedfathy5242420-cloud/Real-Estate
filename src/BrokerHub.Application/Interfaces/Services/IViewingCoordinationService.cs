using BrokerHub.Application.DTOs.Viewings;

namespace BrokerHub.Application.Interfaces.Services;
public interface IViewingCoordinationService
{
    Task<ViewingResult<ViewingReply>> ConfirmAsync(Guid customerId, Guid id, ConfirmViewingDto request, string key, CancellationToken ct);
    Task<ViewingResult<ViewingPage<StaffViewingDto>>> ListAsync(Guid? customerId, int page, int size, string? status, CancellationToken ct);
    Task<ViewingResult<StaffViewingDto>> GetAsync(Guid customerId, Guid id, CancellationToken ct);
    Task<ViewingResult<ViewingPage<StaffViewingEventDto>>> HistoryAsync(Guid customerId, Guid id, int page, int size, CancellationToken ct);
    Task<ViewingResult<StaffViewingEventDto>> EventAsync(Guid customerId, Guid id, Guid eventId, CancellationToken ct);
}
