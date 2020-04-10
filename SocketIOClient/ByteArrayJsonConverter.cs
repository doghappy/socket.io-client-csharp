using Newtonsoft.Json;
using SocketIOClient.Parsers;
using System;
using System.Linq;

namespace SocketIOClient
{
    public class ByteArrayJsonConverter : JsonConverter
    {
        public ByteArrayJsonConverter(ParserContext ctx)
        {
            _ctx = ctx;
        }

        readonly ParserContext _ctx;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var source = (value as byte[]).ToList();
            source.Insert(0, 4);
            _ctx.SendBuffers.Add(source.ToArray());
            writer.WriteStartObject();
            writer.WritePropertyName("_placeholder");
            writer.WriteValue(true);
            writer.WritePropertyName("num");
            writer.WriteValue(++_ctx.SendBufferCount);
            writer.WriteEndObject();
        }
    }
}
