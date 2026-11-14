using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BrokerHub.Api.Contracts.Units;

public sealed record CreateUnitRequest(
    Guid ProjectId,
    [Required, MaxLength(64)] string Code,
    [Required, MaxLength(200)] string Title,
    [MaxLength(4000)] string? Description,
    [Required, MaxLength(100)] string UnitType,
    [property: JsonRequired] decimal AreaSqm,
    int? Bedrooms,
    int? Bathrooms,
    [property: JsonRequired] decimal ListPrice,
    [Required, MaxLength(3)] string Currency);

public sealed record UpdateUnitRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(4000)] string? Description,
    [Required, MaxLength(100)] string UnitType,
    [property: JsonRequired] decimal AreaSqm,
    int? Bedrooms,
    int? Bathrooms,
    [property: JsonRequired] decimal ListPrice,
    [Required, MaxLength(3)] string Currency,
    [Range(1, long.MaxValue)] long Version);
