using BrokerHub.Application.DTOs.MarketingAgreements;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Mappings;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class MarketingAgreementQueryService(ApplicationDbContext db) : IMarketingAgreementQueryService
{
    public async Task<MarketingAgreementDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var value = await db.MarketingAgreements.AsNoTracking().Include(a => a.Projects).Include(a => a.Rules)
            .AsSingleQuery().SingleOrDefaultAsync(a => a.Id == id, ct);
        return value?.ToDto();
    }
    public async Task<IReadOnlyList<MarketingAgreementDto>> ListAsync(Guid? developerId, int skip, int take, CancellationToken ct)
    {
        var values = await db.MarketingAgreements.AsNoTracking().Where(a => !developerId.HasValue || a.DeveloperId == developerId.Value)
            .OrderBy(a => a.AgreementNumber).ThenBy(a => a.Id).Skip(skip).Take(take)
            .Include(a => a.Projects).Include(a => a.Rules).AsSingleQuery().ToListAsync(ct);
        return values.Select(a => a.ToDto()).ToArray();
    }
}
