using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BrokerHub.Api.Contracts.Customers;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AssignCustomerRequest
{
    [Required] public Guid? EmployeeId { get; init; }
    [Required, Range(1, long.MaxValue)] public long? Version { get; init; }
    [StringLength(2000)] public string? Reason { get; init; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class TransferCustomerRequest
{
    [Required] public Guid? EmployeeId { get; init; }
    [Required, Range(1, long.MaxValue)] public long? Version { get; init; }
    [Required, StringLength(2000)] public string Reason { get; init; } = "";
}
