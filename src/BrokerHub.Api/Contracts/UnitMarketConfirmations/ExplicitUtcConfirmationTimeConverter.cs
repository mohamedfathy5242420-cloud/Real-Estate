using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BrokerHub.Api.Contracts.UnitMarketConfirmations;

// Property-scoped: inspect the wire text before an omitted offset can acquire local time.
public sealed partial class ExplicitUtcConfirmationTimeConverter : JsonConverter<DateTimeOffset>
{
    [GeneratedRegex(@"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(?:\.[0-9]{1,7})?(?:Z|\+00:00)\z", RegexOptions.CultureInvariant)]
    private static partial Regex UtcTimestamp();

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("ConfirmedAtUtc requires a UTC date and time with Z or +00:00.");
        var text = reader.GetString()!;
        if (!UtcTimestamp().IsMatch(text) ||
            !DateTimeOffset.TryParseExact(text,
                ["yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
                 "yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"],
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var instant))
            throw new JsonException("ConfirmedAtUtc requires a valid UTC date and time with Z or +00:00.");
        return instant;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
