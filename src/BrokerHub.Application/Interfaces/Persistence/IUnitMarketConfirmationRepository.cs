using BrokerHub.Application.DTOs.UnitMarketConfirmations;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Application.Interfaces.Persistence;
public interface IUnitMarketConfirmationRepository
{
    Task AddAsync(UnitMarketConfirmation confirmation, CancellationToken ct);
    Task<IReadOnlyList<UnitMarketConfirmationDto>> ListAsync(Guid unitId, int skip, int take, CancellationToken ct);
}
