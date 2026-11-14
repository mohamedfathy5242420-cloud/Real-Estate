namespace BrokerHub.Application.DTOs.Deals;

// Exact saved scalars only. Money/rate strings preserve decimal precision for browser clients.
public sealed record CommissionSnapshotDto(Guid Id,Guid UnitId,Guid LeadId,long LeadVersion,
    Guid CustomerId,Guid ProjectId,Guid DeveloperId,Guid AgreementId,long AgreementVersion,
    string AgreementNumber,string AgreementScope,DateOnly AgreementStartsOn,DateOnly AgreementEndsOn,
    int ProtectionPeriodDays,DateOnly ProtectionEndsOn,DateOnly LeadOpenedOnLocalDate,string OpeningTimeZoneId,
    Guid RuleId,long RuleVersion,Guid? RuleProjectId,string RuleType,string? Percentage,string? FixedAmount,
    string CurrencyCode,DateOnly SaleLocalDate,string FinalUnitPrice,string Amount,DateTime SaleOccurredAtUtc,
    string Source,string Reference,string? SelectionReason,DateTime AdoptedAtUtc,Guid AdoptedByAccountId,long Version);
