using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BrokerHub.Api.Contracts.Customers;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SubmitCustomerLinkRequest
{
    [Required, StringLength(2000)] public string Claim { get; init; } = "";
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ApproveCustomerLinkRequest
{
    [Required, Range(1, long.MaxValue)] public long? Version { get; init; }
    [Required] public Guid? CustomerId { get; init; }
    [Required, Range(1, long.MaxValue)] public long? CustomerVersion { get; init; }
    [Required, StringLength(2000)] public string Reason { get; init; } = "";
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RejectCustomerLinkRequest
{
    [Required, Range(1, long.MaxValue)] public long? Version { get; init; }
    [Required, StringLength(2000)] public string Reason { get; init; } = "";
}
