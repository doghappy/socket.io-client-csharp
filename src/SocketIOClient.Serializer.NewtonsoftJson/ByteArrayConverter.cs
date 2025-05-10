using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SocketIOClient.Serializer.NewtonsoftJson;

public class ByteArrayConverter : JsonConverter
{
    private const string Placeholder = "_placeholder";
    private const string Num = "num";
    public IList<byte[]> Bytes { get; set; } = [];

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
            return;
        Bytes.Add((byte[])value);
        writer.WriteStartObject();
        writer.WritePropertyName(Placeholder);
        writer.WriteValue(true);
        writer.WritePropertyName(Num);
        writer.WriteValue(Bytes.Count - 1);
        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
            return null;
        reader.Read();
        if (reader.TokenType != JsonToken.PropertyName || reader.Value?.ToString() != Placeholder)
            return null;
        reader.Read();
        if (reader.TokenType != JsonToken.Boolean || !(bool)reader.Value)
            return null;
        reader.Read();
        if (reader.TokenType != JsonToken.PropertyName || reader.Value?.ToString() != Num)
            return null;
        reader.Read();
        if (reader.Value == null)
            return null;
        if (!int.TryParse(reader.Value.ToString(), out var num))
            return null;
        var bytes = Bytes[num];
        reader.Read();
        return bytes;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(byte[]);
    }
}