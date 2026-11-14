namespace BrokerHub.Application.DTOs.Catalog;

public sealed record CatalogSearchDto(string? Q = null, string? Area = null, string? UnitType = null,
    Guid? ProjectId = null, Guid? DeveloperId = null, int? Bedrooms = null,
    decimal? MinPrice = null, decimal? MaxPrice = null, string? Currency = null,
    decimal? MinArea = null, decimal? MaxArea = null, string Sort = "newest", int Page = 1, int PageSize = 20);

public sealed record PublicAvailabilityDto(string Status, string Freshness, DateTime? ConfirmedAtUtc,
    bool RequiresExternalConfirmation = true);
public sealed record PublicCatalogImageDto(Guid Id, string? AltText, int SortOrder, string ContentUrl);
public sealed record PublicUnitSummaryDto(Guid Id, string Title, string UnitType, decimal AreaSqm,
    int? Bedrooms, int? Bathrooms, decimal ListPrice, string Currency, Guid ProjectId, string ProjectName,
    string Area, Guid DeveloperId, string DeveloperName, PublicAvailabilityDto Availability,
    PublicCatalogImageDto? PreviewImage);
public sealed record PublicUnitDetailDto(Guid Id, string Title, string? Description, string UnitType,
    decimal AreaSqm, int? Bedrooms, int? Bathrooms, decimal ListPrice, string Currency, Guid ProjectId,
    string ProjectName, string Area, string? Address, string TimeZoneId, Guid DeveloperId,
    string DeveloperName, PublicAvailabilityDto Availability, IReadOnlyList<PublicCatalogImageDto> Images);
public sealed record PublicCatalogPageDto(IReadOnlyList<PublicUnitSummaryDto> Items, int TotalCount, int Page, int PageSize);
public enum PublicCatalogStatus { Success, Invalid, NotFound, Unavailable }
public sealed record PublicCatalogResult<T>(PublicCatalogStatus Status, T? Value = default,
    IReadOnlyDictionary<string, string[]>? Errors = null);
