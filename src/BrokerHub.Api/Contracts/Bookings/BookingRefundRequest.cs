using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Api.Contracts.Bookings;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record BookingRefundRequest([Required,Range(1,long.MaxValue)] long? Version,
    [Required] decimal? Amount,[Required,MaxLength(16)] string CurrencyCode,
    [Required] [property:JsonConverter(typeof(ExplicitUtcBookingTimeConverter))] DateTimeOffset? ReturnedAtUtc,
    [Required,MaxLength(BookingRefund.SourceMaxLength)] string Source,
    [Required,MaxLength(BookingRefund.ReferenceMaxLength)] string Reference,
    [MaxLength(BookingRefund.NotesMaxLength)] string? Notes = null);
