using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;

public static class CustomerAssignmentMappings
{
    public static CrmCustomerDto ToCrmDto(this Customer c) => new(c.Id, c.FullName, c.Phone, c.Email,
        c.PreferredContactMethod, c.Requirements, c.AssignedEmployeeId, c.Version,
        DateTime.SpecifyKind(c.CreatedAtUtc, DateTimeKind.Utc), c.UpdatedAtUtc is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Utc) : null);
    public static CustomerAssignmentDto ToAssignmentDto(this CustomerAssignment a) => new(a.Id, a.CustomerId,
        a.PreviousEmployeeId, a.NewEmployeeId, a.ActorAccountId, DateTime.SpecifyKind(a.ChangedAtUtc, DateTimeKind.Utc), a.Reason, a.CustomerVersion);
}
