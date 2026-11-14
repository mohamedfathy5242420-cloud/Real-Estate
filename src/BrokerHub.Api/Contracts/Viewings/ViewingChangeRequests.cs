using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Api.Contracts.Viewings;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CancelViewingRequest([Required, Range(1,long.MaxValue)] long? Version,
    [Required, MaxLength(Viewing.ChangeReasonMaxLength)] string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ChangeViewingProposalRequest([Required, Range(1,long.MaxValue)] long? Version,
    [Required] [property: JsonConverter(typeof(ExplicitUtcCoordinationTimeConverter))] DateTimeOffset? ProposedStartUtc,
    [Required, MaxLength(Viewing.ChangeReasonMaxLength)] string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CancelStaffViewingRequest([Required, Range(1,long.MaxValue)] long? Version,
    [Required, RegularExpression("^(Broker|Developer)$")] string CancellationKind,
    [Required, MaxLength(Viewing.ChangeReasonMaxLength)] string Reason);
