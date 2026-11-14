using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrokerHub.Api.Serialization;

public sealed class LosslessInt64Converter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader,Type typeToConvert,JsonSerializerOptions options)
    {
        if(reader.TokenType==JsonTokenType.Number&&reader.TryGetInt64(out var number))return number;
        if(reader.TokenType==JsonTokenType.String&&long.TryParse(reader.GetString(),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out number))return number;
        throw new JsonException("An exact Int64 integer number or string is required.");
    }
    public override void Write(Utf8JsonWriter writer,long value,JsonSerializerOptions options)=>writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}
