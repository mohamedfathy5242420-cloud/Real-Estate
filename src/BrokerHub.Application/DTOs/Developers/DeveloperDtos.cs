namespace BrokerHub.Application.DTOs.Developers;

public sealed record DeveloperDto(
    Guid Id,
    string Code,
    string Name,
    string? LegalName,
    string? ExternalContactName,
    string? ExternalContactEmail,
    string? ExternalContactPhone,
    bool IsActive,
    DateTime CreatedAtUtc,
    Guid CreatedByAccountId,
    DateTime? UpdatedAtUtc,
    Guid? UpdatedByAccountId,
    long Version);

public sealed record CreateDeveloperDto(
    string Code,
    string Name,
    string? LegalName,
    string? ExternalContactName,
    string? ExternalContactEmail,
    string? ExternalContactPhone);

public sealed record UpdateDeveloperDto(
    string Name,
    string? LegalName,
    string? ExternalContactName,
    string? ExternalContactEmail,
    string? ExternalContactPhone,
    long Version);
