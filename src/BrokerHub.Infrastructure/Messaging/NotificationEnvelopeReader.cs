using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using BrokerHub.Application.DTOs.Messaging;

namespace BrokerHub.Infrastructure.Messaging;

public static class NotificationEnvelopeReader
{
    private static readonly HashSet<string> Fields=new(StringComparer.Ordinal)
    {
        "envelopeVersion","messageId","sourceStore","sourceEventId","name","eventSchemaVersion",
        "aggregateId","actorAccountId","systemSource","resourceId","resourceVersion","occurredAtUtc","recordedAtUtc","payload"
    };
    public static bool TryRead(ReadOnlyMemory<byte> body,NotificationWireProperties properties,
        [NotNullWhen(true)]out ValidatedNotificationEnvelope? result)
    {
        result=null;
        if(body.Length is <1 or >OutboxEnvelopeWire.MaxBytes||properties.ContentType!="application/json"||!properties.Persistent)return false;
        try
        {
            using var document=JsonDocument.Parse(body,new JsonDocumentOptions{MaxDepth=16});var root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object)return false;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var field in root.EnumerateObject())if(!Fields.Contains(field.Name)||!seen.Add(field.Name))return false;
            if(seen.Count!=Fields.Count||root.GetProperty("envelopeVersion").GetInt32()!=1||root.GetProperty("eventSchemaVersion").GetInt32()!=1)return false;
            var messageId=GuidValue(root.GetProperty("messageId"));var name=root.GetProperty("name").GetString();
            if(name is null||properties.Type!=name||properties.MessageId!=messageId.ToString("N"))return false;
            var definition=NotificationEventCatalog.Find(name);if(definition is null)return false;
            var payload=root.GetProperty("payload");if(payload.ValueKind!=JsonValueKind.Object)return false;
            var systemSource=NullableString(root.GetProperty("systemSource"));
            if(systemSource!=definition.SystemSource)return false;
            var envelope=new OutboxEnvelope(1,messageId,root.GetProperty("sourceStore").GetString()??"",
                GuidValue(root.GetProperty("sourceEventId")),name,1,GuidValue(root.GetProperty("aggregateId")),
                NullableGuid(root.GetProperty("actorAccountId")),systemSource,NullableGuid(root.GetProperty("resourceId")),
                NullableLong(root.GetProperty("resourceVersion")),Utc(root.GetProperty("occurredAtUtc")),
                Utc(root.GetProperty("recordedAtUtc")),payload.GetRawText());
            if(!envelope.IsValid()||(definition.SystemSource is not null)!=(envelope.SourceStore=="SystemOutboxMessages"))return false;
            result=new(envelope,Convert.ToHexString(SHA256.HashData(body.Span)));return true;
        }
        catch(Exception ex)when(ex is JsonException or FormatException or InvalidOperationException or OverflowException or ArgumentException)
        {return false;}
    }
    private static Guid GuidValue(JsonElement field)=>field.ValueKind==JsonValueKind.String&&Guid.TryParseExact(field.GetString(),"D",out var id)&&id!=Guid.Empty?
        id:throw new FormatException("Invalid message identity.");
    private static Guid? NullableGuid(JsonElement field)=>field.ValueKind==JsonValueKind.Null?null:GuidValue(field);
    private static long? NullableLong(JsonElement field)=>field.ValueKind==JsonValueKind.Null?null:field.GetInt64();
    private static string? NullableString(JsonElement field)=>field.ValueKind==JsonValueKind.Null?null:field.GetString();
    private static DateTime Utc(JsonElement field)
    {
        var text=field.GetString();
        if(text is null||!text.EndsWith('Z')||!field.TryGetDateTime(out var at)||at==default||at.Kind!=DateTimeKind.Utc)
            throw new FormatException("Explicit message UTC Z required.");
        return at;
    }
}
