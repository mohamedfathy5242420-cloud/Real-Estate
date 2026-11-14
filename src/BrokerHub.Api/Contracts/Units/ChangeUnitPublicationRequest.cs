using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace BrokerHub.Api.Contracts.Units;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ChangeUnitPublicationRequest(
    [Required, Range(1, long.MaxValue)] long? Version,
    [MaxLength(500)] string? Reason);
