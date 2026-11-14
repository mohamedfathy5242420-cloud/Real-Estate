using System.ComponentModel.DataAnnotations;

namespace BrokerHub.Api.Contracts.Developers;

public sealed record CreateDeveloperRequest(
    [Required, MaxLength(64)] string Code,
    [Required, MaxLength(200)] string Name,
    [MaxLength(300)] string? LegalName,
    [MaxLength(200)] string? ExternalContactName,
    [MaxLength(320)] string? ExternalContactEmail,
    [MaxLength(32)] string? ExternalContactPhone);

public sealed record UpdateDeveloperRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(300)] string? LegalName,
    [MaxLength(200)] string? ExternalContactName,
    [MaxLength(320)] string? ExternalContactEmail,
    [MaxLength(32)] string? ExternalContactPhone,
    [Range(1, long.MaxValue)] long Version);

public sealed record ChangeDeveloperStatusRequest(
    [Range(1, long.MaxValue)] long Version);
