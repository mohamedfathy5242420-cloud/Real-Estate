using System.Linq.Expressions;
using BrokerHub.Application.DTOs.Units;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;

public static class UnitMappings
{
    public static Expression<Func<Unit, UnitDto>> Projection => unit =>
        new UnitDto(
            unit.Id,
            unit.ProjectId,
            unit.Code,
            unit.Title,
            unit.Description,
            unit.UnitType,
            unit.AreaSqm,
            unit.Bedrooms,
            unit.Bathrooms,
            unit.ListPrice,
            unit.Currency,
            unit.PublicationStatus,
            unit.AvailabilityStatus,
            unit.LastPriceConfirmedAtUtc.HasValue ? DateTime.SpecifyKind(unit.LastPriceConfirmedAtUtc.Value, DateTimeKind.Utc) : null,
            unit.LastPriceSource,
            unit.LastAvailabilityConfirmedAtUtc.HasValue ? DateTime.SpecifyKind(unit.LastAvailabilityConfirmedAtUtc.Value, DateTimeKind.Utc) : null,
            unit.LastAvailabilitySource,
            DateTime.SpecifyKind(unit.CreatedAtUtc, DateTimeKind.Utc),
            unit.CreatedByAccountId,
            unit.UpdatedAtUtc.HasValue ? DateTime.SpecifyKind(unit.UpdatedAtUtc.Value, DateTimeKind.Utc) : null,
            unit.UpdatedByAccountId,
            unit.Version,
            unit.LatestPriceConfirmationId, unit.ConfirmedPrice, unit.ConfirmedCurrency,
            unit.LatestAvailabilityConfirmationId,
            unit.LatestPriceConfirmationId.HasValue && unit.ConfirmedPrice == unit.ListPrice && unit.ConfirmedCurrency == unit.Currency);

    public static UnitDto ToDto(this Unit unit) =>
        new(
            unit.Id,
            unit.ProjectId,
            unit.Code,
            unit.Title,
            unit.Description,
            unit.UnitType,
            unit.AreaSqm,
            unit.Bedrooms,
            unit.Bathrooms,
            unit.ListPrice,
            unit.Currency,
            unit.PublicationStatus,
            unit.AvailabilityStatus,
            unit.LastPriceConfirmedAtUtc.HasValue ? DateTime.SpecifyKind(unit.LastPriceConfirmedAtUtc.Value, DateTimeKind.Utc) : null,
            unit.LastPriceSource,
            unit.LastAvailabilityConfirmedAtUtc.HasValue ? DateTime.SpecifyKind(unit.LastAvailabilityConfirmedAtUtc.Value, DateTimeKind.Utc) : null,
            unit.LastAvailabilitySource,
            DateTime.SpecifyKind(unit.CreatedAtUtc, DateTimeKind.Utc),
            unit.CreatedByAccountId,
            unit.UpdatedAtUtc.HasValue ? DateTime.SpecifyKind(unit.UpdatedAtUtc.Value, DateTimeKind.Utc) : null,
            unit.UpdatedByAccountId,
            unit.Version,
            unit.LatestPriceConfirmationId, unit.ConfirmedPrice, unit.ConfirmedCurrency,
            unit.LatestAvailabilityConfirmationId,
            unit.LatestPriceConfirmationId.HasValue && unit.ConfirmedPrice == unit.ListPrice && unit.ConfirmedCurrency == unit.Currency);
}
