using System;
using Newtonsoft.Json;
using SocketIOClient.JsonSerializer;
using System.Collections.Generic;

namespace SocketIOClient.Newtonsoft.Json
{
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        public NewtonsoftJsonSerializer() : this(new JsonSerializerSettings())
        {
        }

        public NewtonsoftJsonSerializer(JsonSerializerSettings settings)
        {
            _settings = settings;
        }

        private readonly JsonSerializerSettings _settings;

        private JsonSerializerSettings NewSettings(ByteArrayConverter converter)
        {
            var settings = new JsonSerializerSettings(_settings);
            settings.Converters.Add(converter);
            return settings;
        }

        public JsonSerializeResult Serialize(object[] data)
        {
            var converter = new ByteArrayConverter();
            var settings = NewSettings(converter);
            string json = JsonConvert.SerializeObject(data, settings);
            return new JsonSerializeResult
            {
                Json = json,
                Bytes = converter.Bytes
            };
        }

        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }

        public object Deserialize(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type, _settings);
        }

        public T Deserialize<T>(string json, IList<byte[]> bytes)
        {
            var converter = new ByteArrayConverter();
            converter.Bytes.AddRange(bytes);
            var settings = NewSettings(converter);
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public object Deserialize(string json, Type type, IList<byte[]> bytes)
        {
            var converter = new ByteArrayConverter();
            converter.Bytes.AddRange(bytes);
            var settings = NewSettings(converter);
            return JsonConvert.DeserializeObject(json, type, settings);
        }
    }
}