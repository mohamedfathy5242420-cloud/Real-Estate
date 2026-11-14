using BrokerHub.Application.DTOs.UnitImages;
using BrokerHub.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;
namespace BrokerHub.Infrastructure.Persistence.Queries;
internal sealed class UnitImageQueryService(ApplicationDbContext context) : IUnitImageQueryService
{
    public Task<UnitImageCollectionDto?> ListAsync(Guid unitId, CancellationToken cancellationToken) =>
        context.Units.AsNoTracking().Where(unit => unit.Id == unitId)
            .Select(unit => new UnitImageCollectionDto(unit.Id, unit.Version,
                context.UnitImages.Where(image => image.UnitId == unit.Id)
                    .OrderBy(image => image.SortOrder).ThenBy(image => image.Id)
                    .Select(image => new UnitImageDto(image.Id, image.UnitId, image.AltText, image.SortOrder, image.IsPrimary,
                        DateTime.SpecifyKind(image.CreatedAtUtc, DateTimeKind.Utc), image.CreatedByAccountId,
                        image.UpdatedAtUtc.HasValue ? DateTime.SpecifyKind(image.UpdatedAtUtc.Value, DateTimeKind.Utc) : (DateTime?)null,
                        image.UpdatedByAccountId)).ToList()))
            .AsSingleQuery().SingleOrDefaultAsync(cancellationToken);

    public Task<UnitImageFileReference?> GetImageAsync(Guid unitId, Guid imageId, CancellationToken cancellationToken) =>
        (from image in context.UnitImages.AsNoTracking()
         join unit in context.Units.AsNoTracking() on image.UnitId equals unit.Id
         where image.UnitId == unitId && image.Id == imageId
         select new UnitImageFileReference(image.StorageKey, new UnitImageMutationDto(unit.Version,
             new UnitImageDto(image.Id, image.UnitId, image.AltText, image.SortOrder, image.IsPrimary,
                 DateTime.SpecifyKind(image.CreatedAtUtc, DateTimeKind.Utc), image.CreatedByAccountId,
                 image.UpdatedAtUtc.HasValue ? DateTime.SpecifyKind(image.UpdatedAtUtc.Value, DateTimeKind.Utc) : (DateTime?)null,
                 image.UpdatedByAccountId))))
        .SingleOrDefaultAsync(cancellationToken);
}
