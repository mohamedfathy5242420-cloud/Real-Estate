namespace BrokerHub.Application.DTOs.MarketingAgreements;

public sealed record CommissionRuleInput(Guid? ProjectId, string RuleType, decimal? Percentage, decimal? FixedAmount, string Currency);
public sealed record CreateMarketingAgreementDto(Guid DeveloperId, string AgreementNumber, string ScopeType,
    DateOnly StartsOn, DateOnly EndsOn, int? ProtectionPeriodDays, IReadOnlyList<Guid>? ProjectIds, IReadOnlyList<CommissionRuleInput>? CommissionRules);
public sealed record UpdateMarketingAgreementDto(string AgreementNumber, string ScopeType, DateOnly StartsOn,
    DateOnly EndsOn, int? ProtectionPeriodDays, IReadOnlyList<Guid>? ProjectIds, IReadOnlyList<CommissionRuleInput>? CommissionRules, long Version);
public sealed record CommissionRuleDto(Guid Id, Guid? ProjectId, string RuleType, decimal? Percentage, decimal? FixedAmount,
    string Currency, DateTime CreatedAtUtc, Guid CreatedByAccountId, DateTime? UpdatedAtUtc, Guid? UpdatedByAccountId, long Version);
public sealed record MarketingAgreementDto(Guid Id, Guid DeveloperId, string AgreementNumber, string ScopeType,
    DateOnly StartsOn, DateOnly EndsOn, int ProtectionPeriodDays, string Status, IReadOnlyList<Guid> ProjectIds,
    IReadOnlyList<CommissionRuleDto> CommissionRules, DateTime CreatedAtUtc, Guid CreatedByAccountId,
    DateTime? UpdatedAtUtc, Guid? UpdatedByAccountId, long Version);
