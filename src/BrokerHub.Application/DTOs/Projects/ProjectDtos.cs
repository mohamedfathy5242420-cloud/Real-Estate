namespace BrokerHub.Application.DTOs.Projects;

public sealed record ProjectDto(
    Guid Id,
    Guid DeveloperId,
    string Code,
    string Name,
    string Area,
    string? Address,
    string TimeZoneId,
    bool IsActive,
    DateTime CreatedAtUtc,
    Guid CreatedByAccountId,
    DateTime? UpdatedAtUtc,
    Guid? UpdatedByAccountId,
    long Version);

public sealed record CreateProjectDto(
    Guid DeveloperId,
    string Code,
    string Name,
    string Area,
    string? Address,
    string TimeZoneId);

public sealed record UpdateProjectDto(
    string Name,
    string Area,
    string? Address,
    string TimeZoneId,
    long Version);
