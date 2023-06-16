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

        public List<T> GetListOfElementsFromRoot<T>(string json)
        {
            if (typeof(T) == typeof(JsonElement))
            {
                return (List<T>)(object)((JsonDocument)System.Text.Json.JsonSerializer.Deserialize(json, typeof(JsonDocument), _options)).RootElement.EnumerateArray().ToList();
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");
        }

        public string GetString<T>(T Json)
        {
            if (typeof(T) == typeof(JsonElement))
            {
                return ((JsonElement)(object)Json).GetString();
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");
        }

        public T GetRootElement<T>(string json)
        {
            if (typeof(T) == typeof(JsonElement))
            {
                return (T)(object)((JsonDocument)System.Text.Json.JsonSerializer.Deserialize(json, typeof(JsonDocument), _options)).RootElement;
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");
        }
        public int GetInt32FromJsonElement<T>(T element, string message, string propertyName)
        {
            if (typeof(T) == typeof(JsonElement))
            {
                var p = ((JsonElement)(object)element).GetProperty(propertyName);
                int val;
                switch (p.ValueKind)
                {
                    case JsonValueKind.String:
                        val = int.Parse(p.GetString());
                        break;
                    case JsonValueKind.Number:
                        val = p.GetInt32();
                        break;
                    default:
                        throw new ArgumentException($"Invalid message: '{message}'");
                }
                return val;
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");
        }

        public T GetProperty<T>(T element, string propertyName)
        {
            if (typeof(T) == typeof(JsonElement))
            {
                var p = ((JsonElement)(object)element).GetProperty(propertyName);
                return ((T)(object)p);
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");
        }

        public List<T> GetListOfElements<T>(T element)
        {
            if (typeof(T) == typeof(JsonElement))
            {
                var p = (JsonElement)(object)element;
                return (List<T>)(object)p.EnumerateArray().ToList();
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");

        }

        public string GetRawText<T>(T element)
        {
            if (typeof(T) == typeof(JsonElement))
            {
                return ((JsonElement)(object)element).GetRawText();
            }
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported in {GetType().Name}.");
        }
    }
}