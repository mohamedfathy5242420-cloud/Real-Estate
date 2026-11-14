using BrokerHub.Application.DTOs.Catalog;
using BrokerHub.Application.DTOs.UnitImages;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Interfaces.Storage;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;

public sealed class PublicCatalogService(IPublicCatalogQueries queries, IPublicUnitImageReader storage, TimeProvider clock, IPublicImageWarmupQueue warmup)
    : IPublicCatalogService
{
    public async Task<PublicCatalogResult<PublicCatalogPageDto>> ListAsync(CatalogSearchDto search, CancellationToken ct)
    {
        search = search with { Q = Clean(search.Q), Area = Clean(search.Area), UnitType = Clean(search.UnitType),
            Currency = Clean(search.Currency)?.ToUpperInvariant() };
        var errors = new Dictionary<string, string[]>();
        void Invalid(bool condition, string key, string message) { if (condition) errors[key] = [message]; }
        Invalid(search.Q?.Length > 200, "q", "Search exceeds 200 characters.");
        Invalid(search.Area?.Length > 200, "area", "Area exceeds 200 characters.");
        Invalid(search.UnitType?.Length > 100, "unitType", "Unit type exceeds 100 characters.");
        Invalid(search.ProjectId == Guid.Empty || search.DeveloperId == Guid.Empty, "id", "Nonempty identifiers required.");
        Invalid(search.Bedrooms < 0, "bedrooms", "Bedrooms must be nonnegative.");
        Invalid(search.Currency is not null && (search.Currency.Length != 3 || search.Currency.Any(c => c is < 'A' or > 'Z')),
            "currency", "Currency requires three ASCII letters.");
        Invalid(!Money(search.MinPrice, false) || !Money(search.MaxPrice, false) || search.MinPrice > search.MaxPrice,
            "price", "Prices must be ordered nonnegative decimal(18,2) bounds.");
        Invalid(!Money(search.MinArea, true) || !Money(search.MaxArea, true) || search.MinArea > search.MaxArea,
            "areaSqm", "Area bounds must be ordered positive decimal(18,2) values.");
        Invalid(search.Sort is not ("newest" or "priceAsc" or "priceDesc" or "areaAsc" or "areaDesc"), "sort", "Unsupported sort.");
        Invalid(search.Currency is null && (search.MinPrice.HasValue || search.MaxPrice.HasValue || search.Sort is "priceAsc" or "priceDesc"),
            "currency", "A currency is required for price comparisons or ordering.");
        Invalid(search.Page is < 1 or > 10000 || search.PageSize is < 1 or > 100, "page", "Page 1..10000 and pageSize 1..100 required.");
        if (errors.Count > 0) return new(PublicCatalogStatus.Invalid, Errors: errors);
        return new(PublicCatalogStatus.Success, await queries.ListAsync(search, clock.GetUtcNow(), ct));
    }
    public async Task<PublicCatalogResult<PublicUnitDetailDto>> DetailAsync(Guid id, CancellationToken ct)
    {
        var value = await queries.DetailAsync(id, clock.GetUtcNow(), ct);
        if (value is not null && value.Images.Count > 0)
            await warmup.OfferAsync(id, value.Images.Select(image => image.Id).ToArray(), ct);
        return new(value is null ? PublicCatalogStatus.NotFound : PublicCatalogStatus.Success, value);
    }
    public async Task<PublicCatalogResult<UnitImageContent>> ImageAsync(Guid id, Guid imageId, CancellationToken ct)
    {
        var key = await queries.ImageKeyAsync(id, imageId, clock.GetUtcNow(), ct);
        if (key is null) return new(PublicCatalogStatus.NotFound);
        try
        {
            var content = await storage.OpenReadAsync(key, ct);
            return new(content is null ? PublicCatalogStatus.NotFound : PublicCatalogStatus.Success, content);
        }
        catch (IOException) { return new(PublicCatalogStatus.Unavailable); }
        catch (UnauthorizedAccessException) { return new(PublicCatalogStatus.Unavailable); }
    }
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    private static bool Money(decimal? value, bool positive) => !value.HasValue ||
        (positive ? value > 0 : value >= 0) && value <= Unit.DecimalMaximum && decimal.Round(value.Value, 2) == value;
}
