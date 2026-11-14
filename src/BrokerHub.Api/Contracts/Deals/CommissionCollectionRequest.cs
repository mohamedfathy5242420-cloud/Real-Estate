using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BrokerHub.Api.Contracts.Bookings;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Api.Contracts.Deals;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CommissionCollectionRequest([Required,Range(1,long.MaxValue)]long? Version,
    [Required][property:JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]decimal? Amount,
    [Required,MaxLength(16)]string CurrencyCode,
    [Required][property:JsonConverter(typeof(ExplicitUtcBookingTimeConverter))]DateTimeOffset? CollectedAtUtc,
    [Required,MaxLength(CommissionCollection.SourceMaxLength)]string Source,
    [Required,MaxLength(CommissionCollection.ReferenceMaxLength)]string Reference,
    [MaxLength(CommissionCollection.NotesMaxLength)]string? Notes=null);
