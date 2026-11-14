using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using BrokerHub.Api.Contracts.UnitMarketConfirmations;

namespace BrokerHub.Api.Contracts.Viewings;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SubmitViewingRequest([Required] Guid? UnitId,
    [Required] [property: JsonConverter(typeof(ExplicitUtcViewingTimeConverter))] DateTimeOffset? ProposedStartUtc);

public sealed class ExplicitUtcViewingTimeConverter : JsonConverter<DateTimeOffset>
{
    private readonly ExplicitUtcConfirmationTimeConverter parser = new();
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try { return parser.Read(ref reader, typeToConvert, options); }
        catch (JsonException) { throw new JsonException("ProposedStartUtc requires a valid UTC date and time with Z or +00:00."); }
    }
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) => parser.Write(writer, value, options);
}
