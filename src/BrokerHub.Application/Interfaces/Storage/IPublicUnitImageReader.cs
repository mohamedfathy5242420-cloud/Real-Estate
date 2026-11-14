using BrokerHub.Application.DTOs.UnitImages;

namespace BrokerHub.Application.Interfaces.Storage;

// Call only after current public image eligibility resolves the original storage key.
public interface IPublicUnitImageReader
{
    Task<UnitImageContent?> OpenReadAsync(string key,CancellationToken cancellationToken);
}
