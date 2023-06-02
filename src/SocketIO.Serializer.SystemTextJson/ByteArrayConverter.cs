using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SocketIO.Serializer.SystemTextJson
{
    internal class ByteArrayConverter : JsonConverter<byte[]>
    {
        public ByteArrayConverter()
        {
            Bytes = new List<byte[]>();
        }

        public List<byte[]> Bytes { get; }

        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) return null;
            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != "_placeholder") return null;
            reader.Read();
            if (reader.TokenType != JsonTokenType.True || !reader.GetBoolean()) return null;
            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != "num") return null;
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
            writer.WritePropertyName("_placeholder");
            writer.WriteBooleanValue(true);
            writer.WritePropertyName("num");
            writer.WriteNumberValue(Bytes.Count - 1);
            writer.WriteEndObject();
        }
    }
}
