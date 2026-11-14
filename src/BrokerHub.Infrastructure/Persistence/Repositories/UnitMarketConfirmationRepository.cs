using BrokerHub.Application.DTOs.UnitMarketConfirmations;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace BrokerHub.Infrastructure.Persistence.Repositories;
internal sealed class UnitMarketConfirmationRepository(ApplicationDbContext db) : IUnitMarketConfirmationRepository
{
    public async Task AddAsync(UnitMarketConfirmation confirmation, CancellationToken ct) => await db.UnitMarketConfirmations.AddAsync(confirmation, ct);
    public async Task<IReadOnlyList<UnitMarketConfirmationDto>> ListAsync(Guid unitId, int skip, int take, CancellationToken ct) =>
        await db.UnitMarketConfirmations.AsNoTracking().Where(c => c.UnitId == unitId)
            .OrderByDescending(c => c.ConfirmedAtUtc).ThenByDescending(c => c.RecordedAtUtc).ThenByDescending(c => c.Id)
            .Skip(skip).Take(take).Select(UnitMarketConfirmationMappings.Projection).ToListAsync(ct);
}
