namespace BrokerHub.Application.DTOs.Customers;

public sealed record CreateCustomerProfileDto(string FullName, string Phone, string? Email,
    string? PreferredContactMethod, string? Requirements);
public sealed record UpdateCustomerProfileDto(string FullName, string Phone, string? Email,
    string? PreferredContactMethod, string? Requirements, long Version);
public sealed record CustomerProfileDto(Guid Id, string FullName, string Phone, string? Email,
    string? PreferredContactMethod, string? Requirements, long Version, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
public enum CustomerProfileStatus { Success, Invalid, NotFound, Conflict, Forbidden }
public sealed record CustomerProfileResult<T>(CustomerProfileStatus Status, T? Value = default,
    IReadOnlyDictionary<string, string[]>? Errors = null);
