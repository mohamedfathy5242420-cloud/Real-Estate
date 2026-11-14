using BrokerHub.Application.DTOs.Customers;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;

public static class CustomerMappings
{
    public static CustomerProfileDto ToProfileDto(this Customer customer) => new(customer.Id,
        customer.FullName, customer.Phone, customer.Email, customer.PreferredContactMethod, customer.Requirements,
        customer.Version, DateTime.SpecifyKind(customer.CreatedAtUtc, DateTimeKind.Utc),
        customer.UpdatedAtUtc.HasValue ? DateTime.SpecifyKind(customer.UpdatedAtUtc.Value, DateTimeKind.Utc) : null);
}
