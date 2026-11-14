using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BrokerHub.Api.Contracts.Bookings;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Api.Contracts.Deals;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AdoptDealRequest([Required]Guid? UnitId,[Required,Range(1,long.MaxValue)]long? UnitVersion,
    [Required]Guid? LeadId,[Required,Range(1,long.MaxValue)]long? LeadVersion,[Required]decimal? FinalUnitPrice,
    [Required,MaxLength(16)]string CurrencyCode,
    [Required][property:JsonConverter(typeof(ExplicitUtcBookingTimeConverter))]DateTimeOffset? SaleOccurredAtUtc,
    [Required,MaxLength(Deal.SourceMaxLength)]string Source,[Required,MaxLength(Deal.ReferenceMaxLength)]string Reference,
    [MaxLength(Deal.SelectionReasonMaxLength)]string? SelectionReason=null,Guid? BookingId=null,
    [Range(1,long.MaxValue)]long? BookingVersion=null);
