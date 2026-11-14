using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Api.Contracts.Customers;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateCustomerProfileRequest(
    [Required, MaxLength(Customer.FullNameMaxLength)] string FullName,
    [Required, MaxLength(Customer.PhoneMaxLength)] string Phone,
    [MaxLength(Customer.EmailMaxLength)] string? Email,
    [MaxLength(Customer.PreferredContactMethodMaxLength)] string? PreferredContactMethod,
    [MaxLength(Customer.RequirementsMaxLength)] string? Requirements);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateCustomerProfileRequest(
    [Required, MaxLength(Customer.FullNameMaxLength)] string FullName,
    [Required, MaxLength(Customer.PhoneMaxLength)] string Phone,
    [MaxLength(Customer.EmailMaxLength)] string? Email,
    [MaxLength(Customer.PreferredContactMethodMaxLength)] string? PreferredContactMethod,
    [MaxLength(Customer.RequirementsMaxLength)] string? Requirements,
    [Required, Range(1, long.MaxValue)] long? Version);
