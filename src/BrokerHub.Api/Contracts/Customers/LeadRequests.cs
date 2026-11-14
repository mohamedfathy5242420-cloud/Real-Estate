using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BrokerHub.Api.Contracts.Customers;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OpenLeadRequest
{
    [Required] public Guid? ProjectId { get; init; }
    [Required, StringLength(100)] public string Source { get; init; } = "";
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CloseLeadRequest
{
    [Required, Range(1, long.MaxValue)] public long? Version { get; init; }
    [Required, StringLength(2000)] public string Reason { get; init; } = "";
}
