using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BrokerHub.Api.Contracts.Employees;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateEmployeeProfileRequest
{
    [Required] public Guid? AccountId { get; init; }
    [Required, StringLength(64)] public string EmployeeNumber { get; init; } = "";
    [Required, StringLength(200)] public string DisplayName { get; init; } = "";
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateEmployeeProfileRequest
{
    [Required, StringLength(200)] public string DisplayName { get; init; } = "";
    [Required] public bool? IsActive { get; init; }
    [Required, Range(1, long.MaxValue)] public long? Version { get; init; }
}
