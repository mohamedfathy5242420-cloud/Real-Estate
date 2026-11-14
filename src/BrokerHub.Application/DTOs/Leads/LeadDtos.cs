using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.DTOs.Leads;

public sealed record LeadDto(Guid Id, Guid CustomerId, Guid ProjectId, Guid MarketingAgreementId,
    long AgreementVersionAtOpening, DateOnly AgreementStartsOn, DateOnly AgreementEndsOn, int ProtectionPeriodDays,
    DateOnly ProtectionEndsOn, DateOnly OpenedOnLocalDate, string OpeningTimeZoneId, DateTime OpenedAtUtc,
    Guid OpenedByAccountId, string Source, string Status, long Version, DateTime? ClosedAtUtc, Guid? ClosedByAccountId, string? CloseReason);
public sealed record LeadEventDto(Guid Id, Guid LeadId, Guid CustomerId, long LeadVersion, string EventType, Guid ActorAccountId, DateTime AtUtc, string? Reason);
public sealed record OpenLeadDto(Guid ProjectId, string Source);
public sealed record CloseLeadDto(long Version, string Reason);
public sealed record LeadReply(Guid Id, Guid CustomerId, long LeadVersion, bool Created, bool IsReplay);
public sealed record LeadOpenResult(CommandStatus Status, Lead? Lead = null, bool Created = false);
public sealed record LeadProjectOrigin(Guid DeveloperId, bool DeveloperActive, bool ProjectActive, string TimeZoneId);
public sealed record LeadAgreementOrigin(Guid Id, long Version, DateOnly StartsOn, DateOnly EndsOn, int ProtectionPeriodDays);
