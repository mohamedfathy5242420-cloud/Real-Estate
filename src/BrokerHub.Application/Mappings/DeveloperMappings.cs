using System.Linq.Expressions;
using BrokerHub.Application.DTOs.Developers;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;

public static class DeveloperMappings
{
    public static Expression<Func<Developer, DeveloperDto>> Projection => developer =>
        new DeveloperDto(
            developer.Id,
            developer.Code,
            developer.Name,
            developer.LegalName,
            developer.ExternalContactName,
            developer.ExternalContactEmail,
            developer.ExternalContactPhone,
            developer.IsActive,
            developer.CreatedAtUtc,
            developer.CreatedByAccountId,
            developer.UpdatedAtUtc,
            developer.UpdatedByAccountId,
            developer.Version);

    public static DeveloperDto ToDto(this Developer developer) =>
        new(
            developer.Id,
            developer.Code,
            developer.Name,
            developer.LegalName,
            developer.ExternalContactName,
            developer.ExternalContactEmail,
            developer.ExternalContactPhone,
            developer.IsActive,
            developer.CreatedAtUtc,
            developer.CreatedByAccountId,
            developer.UpdatedAtUtc,
            developer.UpdatedByAccountId,
            developer.Version);
}
