using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SocketIOClient.JsonSerializer
{
    class ByteArrayConverter : JsonConverter<byte[]>
    {
        public ByteArrayConverter(int eio)
        {
            this.eio = eio;
            Bytes = new List<byte[]>();
        }


        readonly int eio;
        internal List<byte[]> Bytes { get; }

        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            byte[] bytes = null;
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "_placeholder")
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.True && reader.GetBoolean())
                    {
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "num")
                        {
                            reader.Read();
                            int num = reader.GetInt32();
                            bytes = Bytes[num];
                            reader.Read();
                            //if (reader. != null)
                            //{
                            //    if (int.TryParse(reader.Value.ToString(), out int num))
                            //    {
                            //        bytes = Bytes[num];
                            //        reader.Read();
                            //    }
                            //}
                        }
                    }
                }
            }
            return bytes;
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            if (eio == 3)
            {
                var bytes = new byte[value.Length + 1];
                bytes[0] = 4;
                value.CopyTo(bytes, 1);
                Bytes.Add(bytes);
            }
            else
            {
                Bytes.Add(value);
            }
            writer.WriteStartObject();
            writer.WritePropertyName("_placeholder");
            writer.WriteBooleanValue(true);
            writer.WritePropertyName("num");
            writer.WriteNumberValue(Bytes.Count - 1);
            writer.WriteEndObject();
        }
    }
}
