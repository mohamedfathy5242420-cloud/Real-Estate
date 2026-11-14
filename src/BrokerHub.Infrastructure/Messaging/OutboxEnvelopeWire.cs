using System.Text.Json;
using BrokerHub.Application.DTOs.Messaging;

namespace BrokerHub.Infrastructure.Messaging;

public static class OutboxEnvelopeWire
{
    public const int MaxBytes=128*1024;
    public static byte[] Serialize(OutboxEnvelope e)
    {
        if(!e.IsValid())throw new ArgumentException("Invalid bounded source envelope.",nameof(e));
        using var buffer=new MemoryStream();using var writer=new Utf8JsonWriter(buffer);
        writer.WriteStartObject();writer.WriteNumber("envelopeVersion",e.EnvelopeVersion);writer.WriteString("messageId",e.MessageId);
        writer.WriteString("sourceStore",e.SourceStore);writer.WriteString("sourceEventId",e.SourceEventId);writer.WriteString("name",e.Name);
        writer.WriteNumber("eventSchemaVersion",e.EventSchemaVersion);writer.WriteString("aggregateId",e.AggregateId);
        if(e.ActorAccountId is { } actor)writer.WriteString("actorAccountId",actor);else writer.WriteNull("actorAccountId");
        writer.WriteString("systemSource",e.SystemSource);
        if(e.ResourceId is { } resource)writer.WriteString("resourceId",resource);else writer.WriteNull("resourceId");
        if(e.ResourceVersion is { } version)writer.WriteNumber("resourceVersion",version);else writer.WriteNull("resourceVersion");
        writer.WriteString("occurredAtUtc",e.OccurredAtUtc);writer.WriteString("recordedAtUtc",e.RecordedAtUtc);
        writer.WritePropertyName("payload");writer.WriteRawValue(e.PayloadJson);writer.WriteEndObject();writer.Flush();
        if(buffer.Length>MaxBytes)throw new ArgumentException("Envelope exceeds bounded wire size.",nameof(e));return buffer.ToArray();
    }
}
