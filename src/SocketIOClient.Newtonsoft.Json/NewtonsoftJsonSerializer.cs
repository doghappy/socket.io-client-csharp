using System;
using Newtonsoft.Json;
using SocketIOClient.JsonSerializer;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;

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

        public List<T> GetListOfElementsFromRoot<T>(string json)
        {
            if (typeof(T) == typeof(JToken))
            {
                return (List<T>)(object)JsonConvert.DeserializeObject<JArray>(json, _settings).Root.AsJEnumerable().ToList();
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");
        }

        public string GetString<T>(T Json)
        {
            if (typeof(T) == typeof(JToken))
            {
                return ((JToken)(object)Json).ToString();
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");
        }

        public T GetRootElement<T>(string json)
        {
            if (typeof(T) == typeof(JToken))
            {
                return (T)(object)JsonConvert.DeserializeObject<JObject>(json,_settings).Root;
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");
        }

        public int GetInt32FromJsonElement<T>(T element, string message, string propertyName)
        {
            if (typeof(T) == typeof(JToken))
            {
                var p = ((JToken)(object)element).SelectToken(propertyName);
                switch (p.Type)
                {
                    case JTokenType.Integer:
                        return p.ToObject<int>();
                    case JTokenType.Float:
                        return int.Parse(p.ToString());
                    case JTokenType.String:
                        return int.Parse(p.ToString());
                    default:
                        throw new ArgumentException($"Invalid message: '{message}'");
                }
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");

        }

        public T GetProperty<T>(T element, string propertyName)
        {
            if (typeof(T) == typeof(JToken))
            {
                var p = ((JToken)(object)element).SelectToken(propertyName);
                return (T)(object)p;
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");

        }

        public List<T> GetListOfElements<T>(T element)
        {
            if (typeof(T) == typeof(JToken))
            {
                var e = ((JToken)(object)element);
                return (List<T>)(object)e.AsJEnumerable().ToList();
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");
        }

        public string GetRawText<T>(T element)
        {
            if (typeof(T) == typeof(JToken))
            {
               return ((JToken)(object)element).ToString(Formatting.None);
            }
            else if(typeof(T) == typeof(JObject))
            {
                return ((JObject)(object)element).ToString(Formatting.None);
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");
        }
    }
}