using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SocketIO.Core;
using SocketIO.Serializer.Core;

namespace SocketIO.Serializer.SystemTextJson
{
    public class SystemTextJsonSerializer : ISerializer
    {
        public SystemTextJsonSerializer() : this(new JsonSerializerOptions())
        {
        }

        public SystemTextJsonSerializer(JsonSerializerOptions options)
        {
            _options = options;
        }

        private readonly JsonSerializerOptions _options;

        private JsonSerializerOptions NewOptions(JsonConverter converter)
        {
            var options = new JsonSerializerOptions(_options);
            options.Converters.Add(converter);
            return options;
        }

        public List<SerializedItem> Serialize(object data)
        {
            throw new NotImplementedException();
        }

        private static List<SerializedItem> NewSerializedItems(StringBuilder builder, IEnumerable<byte[]> bytes)
        {
            var result = new List<SerializedItem>
            {
                new SerializedItem
                {
                    Type = SerializedMessageType.Text,
                    Text = builder.ToString()
                }
            };
            result.AddRange(bytes.Select(x => new SerializedItem
            {
                Type = SerializedMessageType.Binary,
                Binary = x
            }));
            return result;
        }

        private static object[] InsertEventToData(string eventName, object[] data)
        {
            var newData = new object[data.Length + 1];
            newData[0] = eventName;
            Array.Copy(data, 0, newData, 1, data.Length);
            return newData;
        }
        
        public List<SerializedItem> Serialize(string eventName, int packetId, string ns, object[] data)
        {
            return InternalSerialize(eventName, packetId, ns, data);
        }

        public List<SerializedItem> Serialize(int packetId, string ns, object[] data)
        {
            var converter = new ByteArrayConverter();
            var options = NewOptions(converter);
            var json = data != null && data.Length > 0
                ? JsonSerializer.Serialize(data, options)
                : "[]";

            var builder = new StringBuilder();
            if (converter.Bytes.Count == 0)
            {
                builder.Append("43");
            }
            else
            {
                builder
                    .Append("46")
                    .Append(converter.Bytes.Count)
                    .Append('-');
            }

            if (!string.IsNullOrEmpty(ns))
            {
                builder.Append(ns).Append(',');
            }

            builder.Append(packetId).Append(json);
            return NewSerializedItems(builder, converter.Bytes);
        }

        public List<SerializedItem> Serialize(string eventName, string ns, object[] data)
        {
            return InternalSerialize(eventName, null, ns, data);
            // var newData = InsertEventToData(eventName, data);
            //
            // var converter = new ByteArrayConverter();
            // var options = NewOptions(converter);
            // var json = JsonSerializer.Serialize(newData, options);
            //
            // var builder = new StringBuilder();
            // if (converter.Bytes.Count == 0)
            // {
            //     builder.Append("42");
            // }
            // else
            // {
            //     builder.Append("45").Append(converter.Bytes.Count).Append('-');
            // }
            //
            // if (!string.IsNullOrEmpty(ns))
            // {
            //     builder.Append(ns);
            // }
            //
            // builder.Append(json);
            // return NewSerializedItems(builder, converter.Bytes);
        }
        
        private List<SerializedItem> InternalSerialize(string eventName, int? packetId, string ns, object[] data)
        {
            var newData = InsertEventToData(eventName, data);

            var converter = new ByteArrayConverter();
            var options = NewOptions(converter);
            var json = JsonSerializer.Serialize(newData, options);

            var builder = new StringBuilder();
            if (converter.Bytes.Count == 0)
            {
                builder.Append("42");
            }
            else
            {
                builder.Append("45").Append(converter.Bytes.Count).Append('-');
            }

            if (!string.IsNullOrEmpty(ns))
            {
                builder.Append(ns);
            }

            if (packetId is not null)
            {
                builder.Append(packetId);
            }

            builder.Append(json);
            return NewSerializedItems(builder, converter.Bytes);
        }

        public T Deserialize<T>(IMessage2 message, int index)
        {
            var item = ((JsonMessage)message).JsonArray[index];
            return item.Deserialize<T>(_options);
        }

        public object Deserialize(string json, Type type)
        {
            return JsonSerializer.Deserialize(json, type, _options);
        }

        public T Deserialize<T>(string json, IEnumerable<byte[]> bytes)
        {
            var converter = new ByteArrayConverter();
            converter.Bytes.AddRange(bytes);
            var options = NewOptions(converter);
            return JsonSerializer.Deserialize<T>(json, options);
        }

        public object Deserialize(string json, Type type, IEnumerable<byte[]> bytes)
        {
            var converter = new ByteArrayConverter();
            converter.Bytes.AddRange(bytes);
            var options = NewOptions(converter);
            return JsonSerializer.Deserialize(json, type, options);
        }

        public IMessage2 Deserialize(EngineIO eio, string text)
        {
            var enums = Enum.GetValues(typeof(MessageType));
            foreach (MessageType type in enums)
            {
                var prefix = ((int)type).ToString();
                if (!text.StartsWith(prefix)) continue;

                var message = NewMessage(type);
                ReadMessage(message, eio, text.Substring(prefix.Length));
                return message;
            }

            return null;
        }

        public string MessageToJson(IMessage2 message)
        {
            return ((JsonMessage)message).JsonArray.ToJsonString(_options);
        }

        public IMessage2 NewMessage(MessageType type)
        {
            return new JsonMessage(type);
        }

        #region Read Message

        private static void ReadMessage(IMessage2 message, EngineIO eio, string text)
        {
            switch (message.Type)
            {
                case MessageType.Opened:
                    ReadOpenedMessage(message, text);
                    break;
                case MessageType.Ping:
                    break;
                case MessageType.Pong:
                    break;
                case MessageType.Connected:
                    break;
                case MessageType.Disconnected:
                    break;
                case MessageType.Event:
                    ReadEventMessage(message, text);
                    break;
                case MessageType.Ack:
                    ReadAckMessage(message, text);
                    break;
                case MessageType.Error:
                    ReadErrorMessage(message, text, eio);
                    break;
                case MessageType.Binary:
                    ReadBinaryMessage(message, text);
                    break;
                case MessageType.BinaryAck:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message.Type), message.Type, null);
            }
        }

        private static void ReadOpenedMessage(IMessage2 message, string text)
        {
            // TODO: Should deserializing to existing object
            // But haven't support yet. https://github.com/dotnet/runtime/issues/78556
            var newMessage = JsonSerializer.Deserialize<JsonMessage>(text, new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            message.Sid = newMessage.Sid;
            message.PingInterval = newMessage.PingInterval;
            message.PingTimeout = newMessage.PingTimeout;
            message.Upgrades = newMessage.Upgrades;
        }

        private static void ReadEventMessage(IMessage2 message, string text)
        {
            var index = text.IndexOf('[');
            var lastIndex = text.LastIndexOf(',', index);
            if (lastIndex > -1)
            {
                var subText = text.Substring(0, index);
                message.Namespace = subText.Substring(0, lastIndex);
                if (index - lastIndex > 1)
                {
                    message.Id = int.Parse(subText.Substring(lastIndex + 1));
                }
            }
            else
            {
                if (index > 0)
                {
                    message.Id = int.Parse(text.Substring(0, index));
                }
            }

            message.ReceivedText = text.Substring(index);
        }

        private static void ReadAckMessage(IMessage2 message, string text)
        {
            var index = text.IndexOf('[');
            var lastIndex = text.LastIndexOf(',', index);
            if (lastIndex > -1)
            {
                var subText = text.Substring(0, index);
                message.Namespace = subText.Substring(0, lastIndex);
                message.Id = int.Parse(subText.Substring(lastIndex + 1));
            }
            else
            {
                message.Id = int.Parse(text.Substring(0, index));
            }

            message.ReceivedText = text.Substring(index);
        }

        private static void ReadErrorMessage(IMessage2 message, string text, EngineIO eio)
        {
            if (eio == EngineIO.V3)
            {
                message.Error = text.Trim('"');
            }
            else
            {
                var index = text.IndexOf('{');
                if (index > 0)
                {
                    message.Namespace = text.Substring(0, index - 1);
                    text = text.Substring(index);
                }

                var jsonNode = JsonNode.Parse(text);
                if (jsonNode is null)
                {
                    throw new InvalidOperationException($"Get a null while parse '{text}' to JsonNode");
                }

                var jsonObject = jsonNode.AsObject();
                if (jsonObject is null)
                {
                    throw new InvalidCastException("Cannot cast JsonNode to JsonObject");
                }

                message.Error = jsonObject["message"]?.GetValue<string>();
            }
        }

        private static void ReadBinaryMessage(IMessage2 message, string text)
        {
            message.ReceivedBinary = new List<byte[]>();
            var index1 = text.IndexOf('-');
            message.BinaryCount = int.Parse(text.Substring(0, index1));

            var index2 = text.IndexOf('[');

            var index3 = text.LastIndexOf(',', index2);
            if (index3 > -1)
            {
                message.Namespace = text.Substring(index1 + 1, index3 - index1 - 1);
                var idLength = index2 - index3 - 1;
                if (idLength > 0)
                {
                    message.Id = int.Parse(text.Substring(index3 + 1, idLength));
                }
            }
            else
            {
                var idLength = index2 - index1 - 1;
                if (idLength > 0)
                {
                    message.Id = int.Parse(text.Substring(index1 + 1, idLength));
                }
            }

            message.ReceivedText = text.Substring(index2);
        }

        #endregion
    }
}