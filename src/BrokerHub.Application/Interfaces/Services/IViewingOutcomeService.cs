using BrokerHub.Application.DTOs.Viewings;
namespace BrokerHub.Application.Interfaces.Services;
public interface IViewingOutcomeService
{
    Task<ViewingResult<ViewingReply>> RecordAsync(Guid customerId, Guid id, RecordViewingOutcomeDto request, string key, CancellationToken ct);
}
