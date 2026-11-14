using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BrokerHub.Application.DTOs.MarketingAgreements;

namespace BrokerHub.Api.Contracts.MarketingAgreements;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ActivateMarketingAgreementRequest([Required, Range(1, long.MaxValue)] long? Version);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateMarketingAgreementRequest(Guid DeveloperId, [Required, MaxLength(64)] string AgreementNumber,
    [Required] string ScopeType, [Required] DateOnly? StartsOn, [Required] DateOnly? EndsOn, int? ProtectionPeriodDays,
    IReadOnlyList<Guid>? ProjectIds, [Required] IReadOnlyList<CommissionRuleInput>? CommissionRules);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateMarketingAgreementRequest([Required, MaxLength(64)] string AgreementNumber,
    [Required] string ScopeType, [Required] DateOnly? StartsOn, [Required] DateOnly? EndsOn, int? ProtectionPeriodDays,
    IReadOnlyList<Guid>? ProjectIds, [Required] IReadOnlyList<CommissionRuleInput>? CommissionRules, [Required, Range(1, long.MaxValue)] long? Version);
