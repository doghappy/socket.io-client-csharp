using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SocketIO.Serializer.NewtonsoftJson
{
    internal class ByteArrayConverter : JsonConverter
    {
        public ByteArrayConverter()
        {
            Bytes = new List<byte[]>();
        }

        public List<byte[]> Bytes { get; }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var source = (byte[])value;
            Bytes.Add(source.ToArray());
            writer.WriteStartObject();
            writer.WritePropertyName("_placeholder");
            writer.WriteValue(true);
            writer.WritePropertyName("num");
            writer.WriteValue(Bytes.Count - 1);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                return null;
            reader.Read();
            if (reader.TokenType != JsonToken.PropertyName || reader.Value?.ToString() != "_placeholder")
                return null;
            reader.Read();
            if (reader.TokenType != JsonToken.Boolean || !(bool)reader.Value)
                return null;
            reader.Read();
            if (reader.TokenType != JsonToken.PropertyName || reader.Value?.ToString() != "num")
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
}