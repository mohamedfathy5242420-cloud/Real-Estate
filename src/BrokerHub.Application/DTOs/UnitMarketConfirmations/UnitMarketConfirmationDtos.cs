using BrokerHub.Application.DTOs.Units;
namespace BrokerHub.Application.DTOs.UnitMarketConfirmations;
public sealed record RecordUnitMarketConfirmationDto(DateTimeOffset ConfirmedAtUtc, string Source,
    string? SourceReference, string? Notes, decimal? ConfirmedPrice, string? Currency, string? AvailabilityStatus, long Version);
public sealed record UnitMarketConfirmationDto(Guid Id, Guid UnitId, DateTime ConfirmedAtUtc,
    DateTime RecordedAtUtc, Guid RecordedByAccountId, string Source, string? SourceReference, string? Notes,
    decimal? ConfirmedPrice, string? Currency, string? AvailabilityStatus);
public sealed record UnitMarketConfirmationMutationDto(UnitDto Unit, UnitMarketConfirmationDto Confirmation);
