using BrokerHub.Application.DTOs.Employees;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;

public static class EmployeeMappings
{
    public static EmployeeProfileDto ToProfileDto(this Employee e) => new(e.Id, e.AccountId, e.EmployeeNumber,
        e.DisplayName, e.IsActive, e.Version, DateTime.SpecifyKind(e.CreatedAtUtc, DateTimeKind.Utc), e.CreatedByAccountId,
        e.UpdatedAtUtc.HasValue ? DateTime.SpecifyKind(e.UpdatedAtUtc.Value, DateTimeKind.Utc) : null, e.UpdatedByAccountId);
    public static EmployeeRevisionDto ToRevisionDto(this EmployeeRevision r) => new(r.Id, r.Version, r.DisplayName,
        r.IsActive, DateTime.SpecifyKind(r.AtUtc, DateTimeKind.Utc), r.ActorAccountId);
}
