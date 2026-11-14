using BrokerHub.Application.DTOs.Viewings;

namespace BrokerHub.Application.Interfaces.Services;
public interface IViewingAlternativeService
{
    Task<ViewingResult<ViewingReply>> OfferAsync(Guid customerId, Guid id, OfferViewingAlternativeDto request, string key, CancellationToken ct);
    Task<ViewingResult<ViewingReply>> AcceptAsync(Guid id, AcceptViewingAlternativeDto request, string key, CancellationToken ct);
}
