using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SocketIOClient.JsonSerializer
{
    public class SystemTextJsonSerializer : IJsonSerializer
    {
        public SystemTextJsonSerializer() : this(new JsonSerializerOptions())
        {
        }

        public SystemTextJsonSerializer(JsonSerializerOptions options)
        {
            _options = options;
        }

        private readonly JsonSerializerOptions _options;

        private JsonSerializerOptions NewOptions(ByteArrayConverter converter)
        {
            var options = new JsonSerializerOptions(_options);
            options.Converters.Add(converter);
            return options;
        }

        public JsonSerializeResult Serialize(object[] data)
        {
            var converter = new ByteArrayConverter();
            var options = NewOptions(converter);
            string json = System.Text.Json.JsonSerializer.Serialize(data, options);
            return new JsonSerializeResult
            {
                Json = json,
                Bytes = converter.Bytes
            };
        }

        public T Deserialize<T>(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, _options);
        }

        public object Deserialize(string json, Type type)
        {
            return System.Text.Json.JsonSerializer.Deserialize(json, type, _options);
        }

        public T Deserialize<T>(string json, IList<byte[]> bytes)
        {
            var converter = new ByteArrayConverter();
            converter.Bytes.AddRange(bytes);
            var options = NewOptions(converter);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, options);
        }

        public object Deserialize(string json, Type type, IList<byte[]> bytes)
        {
            var converter = new ByteArrayConverter();
            converter.Bytes.AddRange(bytes);
            var options = NewOptions(converter);
            return System.Text.Json.JsonSerializer.Deserialize(json, type, options);
        }
    }
}