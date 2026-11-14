using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BrokerHub.Api.Serialization;

// Angular must not pass decimal(18,2) through IEEE754 Number. Reject rounding on input too.
public sealed partial class LosslessDecimalConverter : JsonConverter<decimal>
{
    [GeneratedRegex(@"\A-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?\z", RegexOptions.CultureInvariant)]
    private static partial Regex FixedPoint();
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text=reader.TokenType switch
        {
            JsonTokenType.String=>reader.GetString(),
            JsonTokenType.Number=>Encoding.UTF8.GetString(reader.HasValueSequence?reader.ValueSequence.ToArray():reader.ValueSpan),
            _=>null
        };
        if(text is null||text.Length>64||!FixedPoint().IsMatch(text)||
            !decimal.TryParse(text,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var value)||
            Canonical(text)!=Canonical(value.ToString(CultureInfo.InvariantCulture)))
            throw new JsonException("An exact fixed-point decimal number or string is required.");
        return value;
    }
    private static string Canonical(string text)
    {
        if(text.Contains('.'))text=text.TrimEnd('0').TrimEnd('.');
        return text=="-0"?"0":text;
    }
    public override void Write(Utf8JsonWriter writer,decimal value,JsonSerializerOptions options)=>writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}
