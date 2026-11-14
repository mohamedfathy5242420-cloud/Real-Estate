using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using BrokerHub.Api.Contracts.UnitMarketConfirmations;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Api.Contracts.Viewings;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfirmViewingRequest([Required, Range(1,long.MaxValue)] long? Version,
    [Required] [property: JsonConverter(typeof(ExplicitUtcCoordinationTimeConverter))] DateTimeOffset? ConfirmedStartUtc,
    [Required] [property: JsonConverter(typeof(ExplicitUtcCoordinationTimeConverter))] DateTimeOffset? ConfirmedEndUtc,
    [Required] [property: JsonConverter(typeof(ExplicitUtcCoordinationTimeConverter))] DateTimeOffset? CoordinatedAtUtc,
    [Required, MaxLength(Viewing.CoordinationSourceMaxLength)] string CoordinationSource,
    [MaxLength(Viewing.CoordinationReferenceMaxLength)] string? CoordinationReference);
public sealed class ExplicitUtcCoordinationTimeConverter : JsonConverter<DateTimeOffset>
{
    private readonly ExplicitUtcConfirmationTimeConverter parser = new();
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try { return parser.Read(ref reader, typeToConvert, options); }
        catch (JsonException) { throw new JsonException("Viewing coordination timestamps require valid UTC date and time with Z or +00:00."); }
    }
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) => parser.Write(writer, value, options);
}
