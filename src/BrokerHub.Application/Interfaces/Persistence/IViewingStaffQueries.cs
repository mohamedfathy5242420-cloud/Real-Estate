using BrokerHub.Application.DTOs.Viewings;

namespace BrokerHub.Application.Interfaces.Persistence;
public interface IViewingStaffQueries
{
    Task<ViewingResult<ViewingPage<StaffViewingDto>>> ListAsync(Guid actor, Guid? customerId, int page, int size, string? status, CancellationToken ct);
    Task<ViewingResult<StaffViewingDto>> GetAsync(Guid actor, Guid customerId, Guid id, CancellationToken ct);
    Task<ViewingResult<ViewingPage<StaffViewingEventDto>>> HistoryAsync(Guid actor, Guid customerId, Guid id, int page, int size, CancellationToken ct);
    Task<ViewingResult<StaffViewingEventDto>> EventAsync(Guid actor, Guid customerId, Guid id, Guid eventId, CancellationToken ct);
}
