using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BrokerHub.Api.Serialization;

public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader,Type typeToConvert,JsonSerializerOptions options)=>UtcDateTimeOffsetConverter.ReadUtc(ref reader).UtcDateTime;
    public override void Write(Utf8JsonWriter writer,DateTime value,JsonSerializerOptions options)=>
        writer.WriteStringValue(value.Kind==DateTimeKind.Local?value.ToUniversalTime():DateTime.SpecifyKind(value,DateTimeKind.Utc));
}
public sealed partial class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    [GeneratedRegex(@"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(?:\.[0-9]{1,7})?(?:Z|\+00:00)\z",RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitUtc();
    internal static DateTimeOffset ReadUtc(ref Utf8JsonReader reader)
    {
        if(reader.TokenType!=JsonTokenType.String)throw new JsonException("An explicit UTC timestamp is required.");
        var raw=reader.GetString()!;
        if(!ExplicitUtc().IsMatch(raw)||!reader.TryGetDateTimeOffset(out var value)||value.Offset!=TimeSpan.Zero)
            throw new JsonException("A valid timestamp with Z or +00:00 is required.");
        return value;
    }
    public override DateTimeOffset Read(ref Utf8JsonReader reader,Type typeToConvert,JsonSerializerOptions options)=>ReadUtc(ref reader);
    public override void Write(Utf8JsonWriter writer,DateTimeOffset value,JsonSerializerOptions options)=>writer.WriteStringValue(value.UtcDateTime);
}
