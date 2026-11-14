using BrokerHub.Application.DTOs.Units;
namespace BrokerHub.Application.DTOs.UnitPublications;
public sealed record ChangeUnitPublicationDto(long Version, string? Reason);
public sealed record UnitPublicationTransitionDto(Guid Id, Guid UnitId, string FromStatus, string ToStatus,
    long UnitVersion, DateTime RecordedAtUtc, Guid RecordedByAccountId, string? Reason,
    Guid? MarketingAgreementId, DateOnly? EligibilityDate, string? EligibilityTimeZoneId);
public sealed record UnitPublicationMutationDto(UnitDto Unit, UnitPublicationTransitionDto Transition);
