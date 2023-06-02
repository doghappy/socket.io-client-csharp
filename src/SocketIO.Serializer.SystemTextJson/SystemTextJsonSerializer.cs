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

        private JsonSerializerOptions NewOptions(ByteArrayConverter converter)
        {
            var options = new JsonSerializerOptions(_options);
            options.Converters.Add(converter);
            return options;
        }

        public List<SerializedItem> Serialize(object data)
        {
            throw new NotImplementedException();
        }

        public List<SerializedItem> Serialize(long packetId, string ns, EngineIO eio, object[] data)
        {
            throw new NotImplementedException();
        }

        public List<SerializedItem> Serialize(string eventName, string ns, object[] data)
        {
            var newData = new object[data.Length + 1];
            newData[0] = eventName;
            Array.Copy(data, 0, newData, 1, data.Length);

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

            builder.Append(json);

            var result = new List<SerializedItem>
            {
                new SerializedItem
                {
                    Type = SerializedMessageType.Text,
                    Text = builder.ToString()
                }
            };
            result.AddRange(converter.Bytes.Select(x => new SerializedItem
            {
                Type = SerializedMessageType.Binary,
                Binary = x
            }));
            return result;
        }

        public T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _options);
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

        public Message Deserialize(EngineIO eio, string text)
        {
            var enums = Enum.GetValues(typeof(MessageType));
            foreach (MessageType type in enums)
            {
                var prefix = ((int)type).ToString();
                if (!text.StartsWith(prefix)) continue;

                var msg = new Message(type);
                ReadMessage(msg, eio, text.Substring(prefix.Length));
                return msg;
            }

            return null;
        }

        public string MessageToJson(Message message)
        {
            var jsonArray = GetJsonArray(message.ReceivedText);
            if (jsonArray.Count > 0)
            {
                jsonArray.RemoveAt(0);
            }

            return jsonArray.ToJsonString(_options);
        }

        public string GetEventName(Message message)
        {
            var jsonArray = GetJsonArray(message.ReceivedText);
            if (jsonArray.Count < 1)
            {
                throw new ArgumentException("Cannot get event name from an empty json array");
            }

            if (jsonArray[0] is null)
            {
                throw new ArgumentException("Event name is null");
            }

            return jsonArray[0].GetValue<string>();
        }

        private static JsonArray GetJsonArray(string json)
        {
            var jsonNode = JsonNode.Parse(json);
            if (jsonNode is null)
            {
                throw new ArgumentException($"Cannot parse '{json}' to JsonNode");
            }

            return jsonNode.AsArray();
        }

        #region Read Message
        private static void ReadMessage(Message msg, EngineIO eio, string text)
        {
            switch (msg.Type)
            {
                case MessageType.Opened:
                    ReadOpenedMessage(msg, text);
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
                    ReadEventMessage(msg, text);
                    break;
                case MessageType.Ack:
                    break;
                case MessageType.Error:
                    break;
                case MessageType.Binary:
                    ReadBinaryMessage(msg, text);
                    break;
                case MessageType.BinaryAck:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(msg.Type), msg.Type, null);
            }
        }

        private static void ReadBinaryMessage(Message msg, string text)
        {
            msg.ReceivedBinary = new List<byte[]>();
            var index1 = text.IndexOf('-');
            msg.BinaryCount = int.Parse(text.Substring(0, index1));

            var index2 = text.IndexOf('[');

            var index3 = text.LastIndexOf(',', index2);
            if (index3 > -1)
            {
                msg.Namespace = text.Substring(index1 + 1, index3 - index1 - 1);
                var idLength = index2 - index3 - 1;
                if (idLength > 0)
                {
                    msg.Id = int.Parse(text.Substring(index3 + 1, idLength));
                }
            }
            else
            {
                var idLength = index2 - index1 - 1;
                if (idLength > 0)
                {
                    msg.Id = int.Parse(text.Substring(index1 + 1, idLength));
                }
            }

            msg.ReceivedText = text.Substring(index2);
        }

        private static void ReadOpenedMessage(Message msg, string text)
        {
            // TODO: Should deserializing to existing object
            // But haven't support yet. https://github.com/dotnet/runtime/issues/78556
            var newMessage = JsonSerializer.Deserialize<Message>(text, new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            msg.Sid = newMessage.Sid;
            msg.PingInterval = newMessage.PingInterval;
            msg.PingTimeout = newMessage.PingTimeout;
            msg.Upgrades = newMessage.Upgrades;
        }
        
        private static void ReadEventMessage(Message msg, string text)
        {
            var index = text.IndexOf('[');
            var lastIndex = text.LastIndexOf(',', index);
            if (lastIndex > -1)
            {
                var subText = text.Substring(0, index);
                msg.Namespace = subText.Substring(0, lastIndex);
                if (index - lastIndex > 1)
                {
                    msg.Id = int.Parse(subText.Substring(lastIndex + 1));
                }
            }
            else
            {
                if (index > 0)
                {
                    msg.Id = int.Parse(text.Substring(0, index));
                }
            }
            msg.ReceivedText = text.Substring(index);
        }
        #endregion
    }

}