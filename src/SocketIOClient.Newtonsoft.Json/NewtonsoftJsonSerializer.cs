using System;
using Newtonsoft.Json;
using SocketIOClient.JsonSerializer;
using System.Collections.Generic;

namespace SocketIOClient.Newtonsoft.Json
{
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        public NewtonsoftJsonSerializer(int eio)
        {
            this.eio = eio;
        }

        readonly int eio;

        public Func<JsonSerializerSettings> JsonSerializerOptions { get; }

        public JsonSerializeResult Serialize(object[] data)
        {
            var converter = new ByteArrayConverter(eio);
            var settings = GetOptions();
            settings.Converters.Add(converter);
            string json = JsonConvert.SerializeObject(data, settings);
            return new JsonSerializeResult
            {
                Json = json,
                Bytes = converter.Bytes
            };
        }

        public T Deserialize<T>(string json)
        {
            var settings = GetOptions();
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public T Deserialize<T>(string json, IList<byte[]> bytes)
        {
            var converter = new ByteArrayConverter(eio);
            converter.Bytes.AddRange(bytes);
            var settings = GetOptions();
            settings.Converters.Add(converter);
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        private JsonSerializerSettings GetOptions()
        {
            JsonSerializerSettings options;
            if (Options != null)
            {
                options = Options();
            }
            else
            {
                options = CreateOptions();
            }
            if (options == null)
            {
                options = new JsonSerializerSettings();
            }
            return options;
        }

        [Obsolete("Use Options instead.")]
        public virtual JsonSerializerSettings CreateOptions()
        {
            return new JsonSerializerSettings();
        }

        public Func<JsonSerializerSettings> Options { get; set; }
    }
}
