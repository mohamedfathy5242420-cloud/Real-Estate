using BrokerHub.Application.DTOs.UnitImages;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Application.Mappings;
public static class UnitImageMappings
{
    public static UnitImageDto ToDto(this UnitImage image) => new(image.Id, image.UnitId, image.AltText,
        image.SortOrder, image.IsPrimary, DateTime.SpecifyKind(image.CreatedAtUtc, DateTimeKind.Utc),
        image.CreatedByAccountId, image.UpdatedAtUtc.HasValue ? DateTime.SpecifyKind(image.UpdatedAtUtc.Value, DateTimeKind.Utc) : null,
        image.UpdatedByAccountId);
}
