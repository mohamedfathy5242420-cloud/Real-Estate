using System.Linq.Expressions;
using BrokerHub.Application.DTOs.Projects;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;

public static class ProjectMappings
{
    public static Expression<Func<Project, ProjectDto>> Projection => project =>
        new ProjectDto(
            project.Id,
            project.DeveloperId,
            project.Code,
            project.Name,
            project.Area,
            project.Address,
            project.TimeZoneId,
            project.IsActive,
            project.CreatedAtUtc,
            project.CreatedByAccountId,
            project.UpdatedAtUtc,
            project.UpdatedByAccountId,
            project.Version);

    public static ProjectDto ToDto(this Project project) =>
        new(
            project.Id,
            project.DeveloperId,
            project.Code,
            project.Name,
            project.Area,
            project.Address,
            project.TimeZoneId,
            project.IsActive,
            project.CreatedAtUtc,
            project.CreatedByAccountId,
            project.UpdatedAtUtc,
            project.UpdatedByAccountId,
            project.Version);
}
