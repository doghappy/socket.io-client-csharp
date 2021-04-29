using System.Collections.Generic;
using System.Text.Json;

namespace SocketIOClient.JsonSerializer
{
    public class SystemTextJsonSerializer : IJsonSerializer
    {
        public SystemTextJsonSerializer(int eio)
        {
            this.eio = eio;
        }

        readonly int eio;

        public JsonSerializeResult Serialize<T>(T data)
        {
            var converter = new ByteArrayConverter(eio);
            var options = CreateOptions();
            options.Converters.Add(converter);
            string json = System.Text.Json.JsonSerializer.Serialize(data, options);
            return new JsonSerializeResult
            {
                Json = json,
                Bytes = converter.Bytes
            };
        }

        public T Deserialize<T>(string json)
        {
            var options = CreateOptions();
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, options);
        }

        public T Deserialize<T>(string json, IList<byte[]> bytes)
        {
            var converter = new ByteArrayConverter(eio);
            var options = CreateOptions();
            options.Converters.Add(converter);
            converter.Bytes.AddRange(bytes);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, options);
        }

        public virtual JsonSerializerOptions CreateOptions()
        {
            return new JsonSerializerOptions();
        }
    }
}
