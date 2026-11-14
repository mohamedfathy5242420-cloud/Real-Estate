using BrokerHub.Application.DTOs.UnitImages;

namespace BrokerHub.Application.Interfaces.Storage;

public sealed record StoredUnitImage(string Key);
public interface IUnitImageStorage
{
    Task<StoredUnitImage> StoreValidatedAsync(Stream content, CancellationToken cancellationToken);
    Task<UnitImageContent?> OpenReadAsync(string key, CancellationToken cancellationToken);
    // Compensation only for the generated object owned by this failed upload, not a delete workflow.
    Task<bool> DiscardCreatedAsync(StoredUnitImage image, CancellationToken cancellationToken);
}
