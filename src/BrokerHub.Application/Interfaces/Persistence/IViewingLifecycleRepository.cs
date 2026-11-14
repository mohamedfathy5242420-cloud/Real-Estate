namespace BrokerHub.Application.Interfaces.Persistence;

public interface IViewingLifecycleRepository
{
    // Existing confirmed resources only, Unit then original Employee, inside the caller's transaction.
    Task<bool> LockReleasedResourcesAsync(Guid unitId, Guid employeeId, CancellationToken ct);
}
