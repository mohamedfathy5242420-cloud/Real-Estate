using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Api.Contracts.Viewings;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ViewingOutcomeRequest([Required, Range(1,long.MaxValue)] long? Version,
    [Required, RegularExpression("^(Completed|CustomerNoShow)$")] string Outcome,
    [Required] [property: JsonConverter(typeof(ExplicitUtcCoordinationTimeConverter))] DateTimeOffset? OccurredAtUtc,
    [Required, MaxLength(SalesNote.BodyMaxLength)] string Notes);
