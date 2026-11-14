using System.ComponentModel.DataAnnotations;

namespace BrokerHub.Api.Contracts.Projects;

public sealed record CreateProjectRequest(
    Guid DeveloperId,
    [Required, MaxLength(64)] string Code,
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(200)] string Area,
    [MaxLength(500)] string? Address,
    [Required, MaxLength(100)] string TimeZoneId);

public sealed record UpdateProjectRequest(
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(200)] string Area,
    [MaxLength(500)] string? Address,
    [Required, MaxLength(100)] string TimeZoneId,
    [Range(1, long.MaxValue)] long Version);

public sealed record ChangeProjectStatusRequest(
    [Range(1, long.MaxValue)] long Version);
