using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Api.Contracts.Viewings;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfferViewingAlternativeRequest([Required, Range(1,long.MaxValue)] long? Version,
    [Required] [property: JsonConverter(typeof(ExplicitUtcCoordinationTimeConverter))] DateTimeOffset? ProposedStartUtc,
    [Required] [property: JsonConverter(typeof(ExplicitUtcCoordinationTimeConverter))] DateTimeOffset? ProposedEndUtc,
    [Required] [property: JsonConverter(typeof(ExplicitUtcCoordinationTimeConverter))] DateTimeOffset? CoordinatedAtUtc,
    [Required, MaxLength(Viewing.CoordinationSourceMaxLength)] string CoordinationSource,
    [MaxLength(Viewing.CoordinationReferenceMaxLength)] string? CoordinationReference,
    [Required, MaxLength(Viewing.AlternativeReasonMaxLength)] string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AcceptViewingAlternativeRequest([Required, Range(1,long.MaxValue)] long? Version);
