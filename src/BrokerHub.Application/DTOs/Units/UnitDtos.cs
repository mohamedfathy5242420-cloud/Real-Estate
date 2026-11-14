namespace BrokerHub.Application.DTOs.Units;

public sealed record UnitDto(
    Guid Id,
    Guid ProjectId,
    string Code,
    string Title,
    string? Description,
    string UnitType,
    decimal AreaSqm,
    int? Bedrooms,
    int? Bathrooms,
    decimal ListPrice,
    string Currency,
    string PublicationStatus,
    string AvailabilityStatus,
    DateTime? LastPriceConfirmedAtUtc,
    string? LastPriceSource,
    DateTime? LastAvailabilityConfirmedAtUtc,
    string? LastAvailabilitySource,
    DateTime CreatedAtUtc,
    Guid CreatedByAccountId,
    DateTime? UpdatedAtUtc,
    Guid? UpdatedByAccountId,
    long Version,
    Guid? LatestPriceConfirmationId,
    decimal? ConfirmedPrice,
    string? ConfirmedCurrency,
    Guid? LatestAvailabilityConfirmationId,
    bool PriceMatchesAdvertisement);

public sealed record CreateUnitDto(
    Guid ProjectId,
    string Code,
    string Title,
    string? Description,
    string UnitType,
    decimal AreaSqm,
    int? Bedrooms,
    int? Bathrooms,
    decimal ListPrice,
    string Currency);

public sealed record UpdateUnitDto(
    string Title,
    string? Description,
    string UnitType,
    decimal AreaSqm,
    int? Bedrooms,
    int? Bathrooms,
    decimal ListPrice,
    string Currency,
    long Version);
