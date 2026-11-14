using BrokerHub.Application.DTOs.MarketingAgreements;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;

public static class MarketingAgreementMappings
{
    public static MarketingAgreementDto ToDto(this MarketingAgreement a) => new(a.Id, a.DeveloperId, a.AgreementNumber,
        a.ScopeType.ToString(), a.StartsOn, a.EndsOn, a.ProtectionPeriodDays, a.Status,
        a.Projects.Where(p => p.IsIncluded).Select(p => p.ProjectId).OrderBy(p => p).ToArray(),
        a.Rules.Where(r => !r.IsArchived).OrderBy(r => r.ProjectId).Select(r => new CommissionRuleDto(r.Id, r.ProjectId,
            r.RuleType.ToString(), r.Percentage, r.FixedAmount, r.Currency, Utc(r.CreatedAtUtc), r.CreatedByAccountId,
            NullableUtc(r.UpdatedAtUtc), r.UpdatedByAccountId, r.Version)).ToArray(),
        Utc(a.CreatedAtUtc), a.CreatedByAccountId, NullableUtc(a.UpdatedAtUtc), a.UpdatedByAccountId, a.Version);
    private static DateTime Utc(DateTime at) => DateTime.SpecifyKind(at, DateTimeKind.Utc);
    private static DateTime? NullableUtc(DateTime? at) => at.HasValue ? Utc(at.Value) : null;
}
