using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;

public sealed record UnitPublicationContext(Unit? Unit, bool DeveloperActive, bool ProjectActive, string? TimeZoneId, Guid DeveloperId,bool HasAdoptedSale=false);

public interface IUnitPublicationRepository
{
    // The callback, eligibility reads and save are inside one protected transaction through commit.
    Task<T> ExecuteAsync<T>(Guid unitId, Func<UnitPublicationContext, CancellationToken, Task<T>> command, CancellationToken ct);
    Task<Guid?> FindEligibleAgreementAsync(Guid developerId, Guid projectId, DateOnly date, CancellationToken ct);
    Task AddAsync(UnitPublicationTransition transition, CancellationToken ct);
}
