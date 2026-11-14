using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BrokerHub.Application.Serialization;

public static class CanonicalCommandJson
{
    public const int MaxBytes = 16384;
    public static bool IsIdentifier(string? value, int max, bool letterFirst = false) =>
        !string.IsNullOrEmpty(value) && value.Length <= max && (!letterFirst || char.IsAsciiLetter(value[0])) &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or ':' or '-');
    public static string Hash(JsonElement payload) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonicalize(payload))));
    public static string Canonicalize(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object || Encoding.UTF8.GetByteCount(payload.GetRawText()) > MaxBytes * 8)
            throw new ArgumentException("A bounded JSON object is required.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { MaxDepth = 32 })) { Write(writer, payload, 1); writer.Flush(); }
        if (stream.Length > MaxBytes) throw new ArgumentException("Canonical JSON exceeds 16KiB.");
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    private static void Write(Utf8JsonWriter w, JsonElement e, int depth)
    {
        if (depth > 32 && e.ValueKind is JsonValueKind.Object or JsonValueKind.Array) throw new ArgumentException("JSON nesting exceeds32.");
        switch (e.ValueKind)
        {
            case JsonValueKind.Object:
                var properties = e.EnumerateObject().ToArray();
                if (properties.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length) throw new ArgumentException("Duplicate JSON properties.");
                w.WriteStartObject(); foreach (var p in properties.OrderBy(p => p.Name, StringComparer.Ordinal)) { w.WritePropertyName(p.Name); Write(w, p.Value, depth + 1); } w.WriteEndObject(); break;
            case JsonValueKind.Array:
                w.WriteStartArray(); foreach (var item in e.EnumerateArray()) Write(w, item, depth + 1); w.WriteEndArray(); break;
            case JsonValueKind.Number: w.WriteRawValue(Number(e.GetRawText())); break;
            case JsonValueKind.String: w.WriteStringValue(e.GetString()); break;
            case JsonValueKind.True: w.WriteBooleanValue(true); break;
            case JsonValueKind.False: w.WriteBooleanValue(false); break;
            case JsonValueKind.Null: w.WriteNullValue(); break;
            default: throw new ArgumentException("Unsupported JSON value.");
        }
    }
    private static string Number(string raw)
    {
        // Exact decimal coefficient/exponent normalization avoids rounding distinct large/tiny intents.
        var negative = raw[0] == '-'; if (negative) raw = raw[1..];
        var e = raw.IndexOfAny(['e', 'E']);
        var exponent = e < 0 ? BigInteger.Zero : BigInteger.Parse(raw[(e + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture);
        var mantissa = e < 0 ? raw : raw[..e]; var point = mantissa.IndexOf('.');
        var fraction = point < 0 ? 0 : mantissa.Length - point - 1;
        var digits = mantissa.Replace(".", "", StringComparison.Ordinal).TrimStart('0');
        if (digits.Length == 0) return "0";
        var coefficient = digits.TrimEnd('0');
        var scale = exponent + digits.Length - coefficient.Length - fraction;
        var pointAt = scale + coefficient.Length; var sign = negative ? "-" : "";
        // Keep bounded ordinary integers/decimals compatible with typed JSON event consumers.
        if (scale >= 0 && pointAt <= MaxBytes) return sign + coefficient + new string('0', (int)scale);
        if (scale < 0 && pointAt > 0) return sign + coefficient[..(int)pointAt] + "." + coefficient[(int)pointAt..];
        if (scale < 0 && pointAt <= 0 && 2 - pointAt + coefficient.Length <= MaxBytes)
            return sign + "0." + new string('0', (int)-pointAt) + coefficient;
        return sign + coefficient[0] + (coefficient.Length > 1 ? "." + coefficient[1..] : "") + "e" + (pointAt - 1).ToString(CultureInfo.InvariantCulture);
    }
}
