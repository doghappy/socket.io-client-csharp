using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SocketIOClient.V2.Serializer.SystemTextJson;

public class ByteArrayConverter : JsonConverter<byte[]>
{
    private const string Placeholder = "_placeholder";
    private const string Num = "num";
    public IList<byte[]> Bytes { get; set; } = [];

    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject) return null;
        reader.Read();
        if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != Placeholder) return null;
        reader.Read();
        if (reader.TokenType != JsonTokenType.True || !reader.GetBoolean()) return null;
        reader.Read();
        if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != Num) return null;
        reader.Read();
        var num = reader.GetInt32();
        var bytes = Bytes[num];
        reader.Read();
        return bytes;
    }

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        Bytes.Add(value);
        writer.WriteStartObject();
        writer.WritePropertyName(Placeholder);
        writer.WriteBooleanValue(true);
        writer.WritePropertyName(Num);
        writer.WriteNumberValue(Bytes.Count - 1);
        writer.WriteEndObject();
    }
}