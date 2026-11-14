using BrokerHub.Application.DTOs.Viewings;

namespace BrokerHub.Application.Interfaces.Services;
public interface IViewingChangeService
{
    Task<ViewingResult<ViewingReply>> CancelOwnAsync(Guid id, CancelViewingDto request, string key, CancellationToken ct);
    Task<ViewingResult<ViewingReply>> ChangeProposalAsync(Guid id, ChangeViewingProposalDto request, string key, CancellationToken ct);
    Task<ViewingResult<ViewingReply>> CancelStaffAsync(Guid customerId, Guid id, CancelStaffViewingDto request, string key, CancellationToken ct);
}
