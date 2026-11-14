namespace BrokerHub.Application.Interfaces.Persistence;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves atomically only while the Developer is active, protecting that condition until commit.
    /// </summary>
    Task SaveChangesWithActiveDeveloperAsync(Guid developerId, CancellationToken cancellationToken);

    /// <summary>Saves only while both persisted catalog parents are active, protected until commit.</summary>
    Task SaveChangesWithActiveCatalogParentsAsync(Guid projectId, Guid developerId, CancellationToken cancellationToken);
}
