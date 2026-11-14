using System.Data;
using System.Text.Json;
using BrokerHub.Application.DTOs.Catalog;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerHub.Infrastructure.Persistence.Queries;

internal sealed class PublicCatalogQueries(ApplicationDbContext db) : IPublicCatalogQueries
{
    // Each read is retried as a whole, with no tracked state or mutations. Short serializable reads
    // keep total/page/gallery and eligibility on the same database view, including under RCSI.
    private Task<T> ReadAsync<T>(Func<Task<T>> read, CancellationToken ct) => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var value = await read();
        await transaction.CommitAsync(ct);
        return value;
    });

    private async Task<IQueryable<Row>> EligibleAsync(DateTimeOffset at, CancellationToken ct)
    {
        var zones = await db.Projects.AsNoTracking().Where(p => p.IsActive).Select(p => p.TimeZoneId).Distinct().ToArrayAsync(ct);
        var dates = new List<object>();
        foreach (var zone in zones)
        {
            try { dates.Add(new { Zone = zone, Date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(at, TimeZoneInfo.FindSystemTimeZoneById(zone)).DateTime) }); }
            catch (TimeZoneNotFoundException) { /* Invalid persisted zone fails closed. */ }
            catch (InvalidTimeZoneException) { /* No host-local fallback. */ }
        }
        var json = JsonSerializer.Serialize(dates);
        var units = db.Units.FromSqlInterpolated($"""
            SELECT u.* FROM [Catalog].[Units] u
            JOIN [Catalog].[Projects] p ON p.[Id]=u.[ProjectId]
            JOIN [Catalog].[Developers] d ON d.[Id]=p.[DeveloperId]
            JOIN OPENJSON({json}) WITH ([Zone] nvarchar(100) '$.Zone', [Date] date '$.Date') z ON z.[Zone]=p.[TimeZoneId]
            WHERE u.[PublicationStatus]='Published' AND p.[IsActive]=1 AND d.[IsActive]=1
              AND NOT EXISTS (SELECT 1 FROM [Sales].[Deals] sale WHERE sale.[UnitId]=u.[Id])
              AND EXISTS (SELECT 1 FROM [Catalog].[MarketingAgreements] a
                WHERE a.[DeveloperId]=d.[Id] AND a.[Status]='Active' AND a.[StartsOn]<=z.[Date] AND a.[EndsOn]>=z.[Date]
                  AND (a.[ScopeType]='AllDeveloperProjects' OR (a.[ScopeType]='SelectedProjects' AND EXISTS (
                    SELECT 1 FROM [Catalog].[AgreementProjects] ap
                    WHERE ap.[MarketingAgreementId]=a.[Id] AND ap.[ProjectId]=p.[Id] AND ap.[IsIncluded]=1))))
            """).AsNoTracking();
        return from u in units
               join p in db.Projects.AsNoTracking() on u.ProjectId equals p.Id
               join d in db.Developers.AsNoTracking() on p.DeveloperId equals d.Id
               select new Row { Id=u.Id, Title=u.Title, Description=u.Description, UnitType=u.UnitType,
                   AreaSqm=u.AreaSqm, Bedrooms=u.Bedrooms, Bathrooms=u.Bathrooms, ListPrice=u.ListPrice,
                   Currency=u.Currency, ProjectId=p.Id, ProjectName=p.Name, Area=p.Area, Address=p.Address,
                   TimeZoneId=p.TimeZoneId, DeveloperId=d.Id, DeveloperName=d.Name, CreatedAtUtc=u.CreatedAtUtc,
                   Availability=u.AvailabilityStatus, ConfirmedAtUtc=u.LastAvailabilityConfirmedAtUtc };
    }
    public Task<PublicCatalogPageDto> ListAsync(CatalogSearchDto s, DateTimeOffset at, CancellationToken ct) => ReadAsync(async () =>
    {
        var query = await EligibleAsync(at, ct);
        if (s.Q is not null) query = query.Where(r => r.Title.Contains(s.Q) || (r.Description != null && r.Description.Contains(s.Q)) ||
            r.ProjectName.Contains(s.Q) || r.Area.Contains(s.Q) || r.DeveloperName.Contains(s.Q));
        if (s.Area is not null) query = query.Where(r => r.Area == s.Area);
        if (s.UnitType is not null) query = query.Where(r => r.UnitType == s.UnitType);
        if (s.ProjectId.HasValue) query = query.Where(r => r.ProjectId == s.ProjectId);
        if (s.DeveloperId.HasValue) query = query.Where(r => r.DeveloperId == s.DeveloperId);
        if (s.Bedrooms.HasValue) query = query.Where(r => r.Bedrooms == s.Bedrooms);
        if (s.Currency is not null) query = query.Where(r => r.Currency == s.Currency);
        if (s.MinPrice.HasValue) query = query.Where(r => r.ListPrice >= s.MinPrice);
        if (s.MaxPrice.HasValue) query = query.Where(r => r.ListPrice <= s.MaxPrice);
        if (s.MinArea.HasValue) query = query.Where(r => r.AreaSqm >= s.MinArea);
        if (s.MaxArea.HasValue) query = query.Where(r => r.AreaSqm <= s.MaxArea);
        var count = await query.CountAsync(ct);
        var ordered = s.Sort switch
        {
            "priceAsc" => query.OrderBy(r => r.ListPrice), "priceDesc" => query.OrderByDescending(r => r.ListPrice),
            "areaAsc" => query.OrderBy(r => r.AreaSqm), "areaDesc" => query.OrderByDescending(r => r.AreaSqm),
            _ => query.OrderByDescending(r => r.CreatedAtUtc)
        };
        var rows = await ordered.ThenBy(r => r.Id).Skip((s.Page - 1) * s.PageSize).Take(s.PageSize).ToArrayAsync(ct);
        var ids = rows.Select(r => r.Id).ToArray();
        var previews = await db.UnitImages.AsNoTracking().Where(i => ids.Contains(i.UnitId))
            .GroupBy(i => i.UnitId).Select(g => g.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).First()).ToArrayAsync(ct);
        return new PublicCatalogPageDto(rows.Select(r => new PublicUnitSummaryDto(r.Id, r.Title, r.UnitType, r.AreaSqm,
            r.Bedrooms, r.Bathrooms, r.ListPrice, r.Currency, r.ProjectId, r.ProjectName, r.Area, r.DeveloperId, r.DeveloperName,
            Availability(r, at), previews.FirstOrDefault(i => i.UnitId == r.Id) is { } image ? Image(image) : null)).ToArray(), count, s.Page, s.PageSize);
    }, ct);

    public Task<PublicUnitDetailDto?> DetailAsync(Guid id, DateTimeOffset at, CancellationToken ct) => ReadAsync<PublicUnitDetailDto?>(async () =>
    {
        var row = await (await EligibleAsync(at, ct)).SingleOrDefaultAsync(r => r.Id == id, ct);
        if (row is null) return null;
        var images = await db.UnitImages.AsNoTracking().Where(i => i.UnitId == id).OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToArrayAsync(ct);
        return new(row.Id, row.Title, row.Description, row.UnitType, row.AreaSqm, row.Bedrooms, row.Bathrooms, row.ListPrice,
            row.Currency, row.ProjectId, row.ProjectName, row.Area, row.Address, row.TimeZoneId, row.DeveloperId, row.DeveloperName,
            Availability(row, at), images.Select(Image).ToArray());
    }, ct);

    public Task<string?> ImageKeyAsync(Guid unitId, Guid imageId, DateTimeOffset at, CancellationToken ct) => ReadAsync(async () =>
    {
        var eligible = await EligibleAsync(at, ct);
        return await (from u in eligible join image in db.UnitImages.AsNoTracking() on u.Id equals image.UnitId
                      where u.Id == unitId && image.Id == imageId select image.StorageKey).SingleOrDefaultAsync(ct);
    }, ct);

    private static PublicCatalogImageDto Image(UnitImage i) => new(i.Id, i.AltText, i.SortOrder, $"/api/catalog/units/{i.UnitId}/images/{i.Id}/content");
    private static PublicAvailabilityDto Availability(Row row, DateTimeOffset at)
    {
        var confirmed = row.ConfirmedAtUtc.HasValue ? DateTime.SpecifyKind(row.ConfirmedAtUtc.Value, DateTimeKind.Utc) : (DateTime?)null;
        return new(row.Availability, confirmed is null ? "unconfirmed" : at.UtcDateTime - confirmed.Value >= TimeSpan.FromDays(7) ? "stale" : "fresh", confirmed);
    }
    private sealed class Row
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = "";
        public string? Description { get; init; }
        public string UnitType { get; init; } = "";
        public decimal AreaSqm { get; init; }
        public int? Bedrooms { get; init; }
        public int? Bathrooms { get; init; }
        public decimal ListPrice { get; init; }
        public string Currency { get; init; } = "";
        public Guid ProjectId { get; init; }
        public string ProjectName { get; init; } = "";
        public string Area { get; init; } = "";
        public string? Address { get; init; }
        public string TimeZoneId { get; init; } = "";
        public Guid DeveloperId { get; init; }
        public string DeveloperName { get; init; } = "";
        public DateTime CreatedAtUtc { get; init; }
        public string Availability { get; init; } = "";
        public DateTime? ConfirmedAtUtc { get; init; }
    }
}
