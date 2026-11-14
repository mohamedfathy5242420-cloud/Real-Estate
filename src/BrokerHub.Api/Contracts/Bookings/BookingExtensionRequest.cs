using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Api.Contracts.Bookings;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record BookingExtensionRequest([Required,Range(1,long.MaxValue)] long? Version,
    [Required] [property:JsonConverter(typeof(ExplicitUtcBookingTimeConverter))] DateTimeOffset? DeveloperExtendedAtUtc,
    [Required] [property:JsonConverter(typeof(ExplicitUtcBookingTimeConverter))] DateTimeOffset? NewReservationExpiresAtUtc,
    [Required,MaxLength(BookingExtension.SourceMaxLength)] string Source,
    [Required,MaxLength(BookingExtension.ReferenceMaxLength)] string Reference,
    [MaxLength(BookingExtension.ReasonMaxLength)] string? Reason = null);
