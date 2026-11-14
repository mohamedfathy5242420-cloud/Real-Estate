using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace BrokerHub.Api.Contracts.UnitMarketConfirmations;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RecordUnitMarketConfirmationRequest(
    [Required] [property: JsonConverter(typeof(ExplicitUtcConfirmationTimeConverter))] DateTimeOffset? ConfirmedAtUtc,
    [Required, MaxLength(200)] string Source, [MaxLength(500)] string? SourceReference,
    [MaxLength(4000)] string? Notes, decimal? ConfirmedPrice, string? Currency, string? AvailabilityStatus,
    [Required, Range(1,long.MaxValue)] long? Version);
