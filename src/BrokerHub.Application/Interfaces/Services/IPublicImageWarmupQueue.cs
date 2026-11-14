using BrokerHub.Application.DTOs.Catalog;

namespace BrokerHub.Application.Interfaces.Services;

public interface IPublicImageWarmupQueue
{
    ValueTask<PublicImageWarmupOffer> OfferAsync(Guid unitId,IReadOnlyList<Guid> imageIds,CancellationToken cancellationToken);
}
