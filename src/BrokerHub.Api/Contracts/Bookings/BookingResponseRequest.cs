using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using BrokerHub.Api.Contracts.UnitMarketConfirmations;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Api.Contracts.Bookings;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record BookingResponseRequest([Required,Range(1,long.MaxValue)] long? Version,
    [Required,RegularExpression("^(Confirmed|Rejected)$")] string ResponseKind,
    [Required] [property:JsonConverter(typeof(ExplicitUtcBookingTimeConverter))] DateTimeOffset? DeveloperRespondedAtUtc,
    [Required,MaxLength(BookingDeveloperResponse.SourceMaxLength)] string Source,
    [Required,MaxLength(BookingDeveloperResponse.ReferenceMaxLength)] string Reference,
    decimal? OfferedPrice = null, [MaxLength(3)] string? CurrencyCode = null,
    [property:JsonConverter(typeof(ExplicitUtcBookingTimeConverter))] DateTimeOffset? OfferedDeadlineUtc = null,
    [MaxLength(BookingDeveloperResponse.ReasonMaxLength)] string? Reason = null);
public sealed class ExplicitUtcBookingTimeConverter : JsonConverter<DateTimeOffset>
{
    private readonly ExplicitUtcConfirmationTimeConverter inner = new();
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        try { return inner.Read(ref reader,type,options); }
        catch (JsonException) { throw new JsonException("Booking times require valid explicit UTC with Z or +00:00."); }
    }
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) => inner.Write(writer,value,options);
}
