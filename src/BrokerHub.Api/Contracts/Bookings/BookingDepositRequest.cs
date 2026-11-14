using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Api.Contracts.Bookings;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record BookingDepositRequest([Required,Range(1,long.MaxValue)] long? Version,
    [Required] decimal? Amount, [Required,MaxLength(16)] string CurrencyCode,
    [Required] [property:JsonConverter(typeof(ExplicitUtcBookingTimeConverter))] DateTimeOffset? PaidAtUtc,
    [Required,MaxLength(BookingDeposit.SourceMaxLength)] string Source,
    [Required,MaxLength(BookingDeposit.ReferenceMaxLength)] string Reference,
    [MaxLength(BookingDeposit.NotesMaxLength)] string? Notes = null);
