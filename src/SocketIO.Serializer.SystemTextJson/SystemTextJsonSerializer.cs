using System;
using System.Collections.Concurrent;
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
            _options = options ?? new JsonSerializerOptions();
            _messageQueue = new ConcurrentQueue<IMessage>();
        }

        private readonly JsonSerializerOptions _options;
        readonly ConcurrentQueue<IMessage> _messageQueue;

        private JsonSerializerOptions NewOptions(JsonConverter converter)
        {
            var options = new JsonSerializerOptions(_options);
            options.Converters.Add(converter);
            return options;
        }

        private string Serialize(object data)
        {
            return JsonSerializer.Serialize(data, _options);
        }

        private static List<SerializedItem> NewSerializedItems(StringBuilder builder, IEnumerable<byte[]> bytes)
        {
            var result = new List<SerializedItem>
            {
                new()
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

        public List<SerializedItem> Serialize(EngineIO _, string eventName, int packetId, string ns, object[] data)
        {
            return InternalSerialize(eventName, packetId, ns, data);
        }

        public List<SerializedItem> Serialize(EngineIO _, int packetId, string nsp, object[] data)
        {
            var converter = new ByteArrayConverter();
            var options = NewOptions(converter);
            var json = data is { Length: > 0 }
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

            if (!string.IsNullOrEmpty(nsp))
            {
                builder.Append(nsp).Append(',');
            }

            builder.Append(packetId).Append(json);
            return NewSerializedItems(builder, converter.Bytes);
        }

        public List<SerializedItem> Serialize(EngineIO _, string eventName, string nsp, object[] data)
        {
            return InternalSerialize(eventName, null, nsp, data);
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
                builder.Append(ns).Append(',');
            }

            if (packetId is not null)
            {
                builder.Append(packetId);
            }

            builder.Append(json);
            return NewSerializedItems(builder, converter.Bytes);
        }

        private (JsonNode jsonNode, JsonSerializerOptions options) GetSerializationData(IMessage message, int index)
        {
            var jsonMessage = (JsonMessage)message;
            var item = jsonMessage.JsonArray[index];
            var converter = new ByteArrayConverter();
            if (jsonMessage.ReceivedBinary is not null)
            {
                converter.Bytes.AddRange(jsonMessage.ReceivedBinary);
            }

            var options = NewOptions(converter);
            return (item, options);
        }

        public T Deserialize<T>(IMessage message, int index)
        {
            var (jsonNode, options) = GetSerializationData(message, index);
            return jsonNode.Deserialize<T>(options);
        }

        public object Deserialize(IMessage message, int index, Type returnType)
        {
            var (jsonNode, options) = GetSerializationData(message, index);
            return jsonNode.Deserialize(returnType, options);
        }

        public IMessage Deserialize(EngineIO eio, string text)
        {
            var enums = Enum.GetValues(typeof(MessageType));
            foreach (MessageType type in enums)
            {
                var prefix = ((int)type).ToString();
                if (!text.StartsWith(prefix)) continue;

                var message = NewMessage(type);
                ReadMessage(message, eio, text.Substring(prefix.Length));

                if (message.BinaryCount > 0)
                {
                    _messageQueue.Enqueue(message);
                    message.ReceivedBinary = new List<byte[]>(message.BinaryCount);
                    return null;
                }

                return message;
            }

            return null;
        }

        public IMessage Deserialize(EngineIO _, byte[] bytes)
        {
            if (_messageQueue.Count <= 0)
                return null;
            if (!_messageQueue.TryPeek(out var msg))
                return null;

            msg.ReceivedBinary.Add(bytes);

            if (msg.ReceivedBinary.Count < msg.BinaryCount)
                return null;

            _messageQueue.TryDequeue(out var result);
            return result;
        }

        public string MessageToJson(IMessage message)
        {
            return ((JsonMessage)message).JsonArray.ToJsonString(_options);
        }

        public IMessage NewMessage(MessageType type)
        {
            return new JsonMessage(type);
        }

        public SerializedItem SerializePingMessage()
        {
            return new SerializedItem
            {
                Text = "2"
            };
        }

        public SerializedItem SerializePingProbeMessage()
        {
            return new SerializedItem
            {
                Text = "2probe"
            };
        }

        public SerializedItem SerializeUpgradeMessage()
        {
            return new SerializedItem
            {
                Text = "5"
            };
        }

        public SerializedItem SerializePongMessage()
        {
            return new SerializedItem
            {
                Text = "3"
            };
        }

        // public SerializedItem SerializeDisconnectionMessage()
        // {
        //     return new SerializedItem
        //     {
        //         Text = "41"
        //     };
        // }

        #region Serialize ConnectedMessage

        public SerializedItem SerializeConnectedMessage(EngineIO eio, string ns, object auth, IEnumerable<KeyValuePair<string, string>> queries)
        {
            return eio switch
            {
                EngineIO.V3 => SerializeEio3ConnectedMessage(ns, queries),
                EngineIO.V4 => SerializeEio4ConnectedMessage(ns, auth),
                _ => throw new ArgumentOutOfRangeException(nameof(EngineIO), eio, null)
            };
        }

        private static SerializedItem SerializeEio3ConnectedMessage(string ns,
            IEnumerable<KeyValuePair<string, string>> queries)
        {
            if (string.IsNullOrEmpty(ns))
            {
                return null;
            }

            var serializedItem = new SerializedItem();
            var builder = new StringBuilder("40");
            builder.Append(ns);
            if (queries != null)
            {
                var i = -1;
                foreach (var item in queries)
                {
                    i++;
                    builder.Append(i == 0 ? '?' : '&');
                    builder.Append(item.Key).Append('=').Append(item.Value);
                }
            }

            builder.Append(',');
            serializedItem.Text = builder.ToString();
            return serializedItem;
        }

        private SerializedItem SerializeEio4ConnectedMessage(string ns, object auth)
        {
            var builder = new StringBuilder("40");
            if (!string.IsNullOrEmpty(ns))
            {
                builder.Append(ns).Append(',');
            }

            if (auth is not null)
            {
                builder.Append(Serialize(auth));
            }

            return new SerializedItem
            {
                Text = builder.ToString()
            };
        }

        #endregion

        #region Read Message

        private static void ReadMessage(IMessage message, EngineIO eio, string text)
        {
            switch (message.Type)
            {
                case MessageType.Opened:
                    ReadOpenedMessage(message, text);
                    break;
                case MessageType.Ping:
                    break;
                case MessageType.Pong:
                    ReadPongMessage(message, text);
                    break;
                case MessageType.Connected:
                    ReadConnectedMessage(message, text, eio);
                    break;
                case MessageType.Disconnected:
                    ReadDisconnectedMessage(message, text);
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
                    ReadBinaryAckMessage(message, text);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message.Type), message.Type, null);
            }
        }

        private static void ReadPongMessage(IMessage message, string text)
        {
            message.ReceivedText = text;
        }

        private static void ReadOpenedMessage(IMessage message, string text)
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

        private static void ReadConnectedMessage(IMessage message, string text, EngineIO eio)
        {
            switch (eio)
            {
                case EngineIO.V3:
                    ReadEio3ConnectedMessage(message, text);
                    break;
                case EngineIO.V4:
                    ReadEio4ConnectedMessage(message, text);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(eio), eio, null);
            }
        }

        private static void ReadDisconnectedMessage(IMessage message, string text)
        {
            message.Namespace = text.TrimEnd(',');
        }

        private static void ReadEio4ConnectedMessage(IMessage message, string text)
        {
            var index = text.IndexOf('{');
            if (index > 0)
            {
                message.Namespace = text.Substring(0, index - 1);
                text = text.Substring(index);
            }
            else
            {
                message.Namespace = string.Empty;
            }

            message.Sid = JsonDocument.Parse(text).RootElement.GetProperty("sid").GetString();
        }

        private static void ReadEio3ConnectedMessage(IMessage message, string text)
        {
            if (text.Length < 2) return;
            var startIndex = text.IndexOf('/');
            if (startIndex == -1)
            {
                return;
            }

            var endIndex = text.IndexOf('?', startIndex);
            if (endIndex == -1)
            {
                endIndex = text.IndexOf(',', startIndex);
            }

            if (endIndex == -1)
            {
                endIndex = text.Length;
            }

            message.Namespace = text.Substring(startIndex, endIndex);
        }

        private static void ReadEventMessage(IMessage message, string text)
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

        private static void ReadAckMessage(IMessage message, string text)
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

        private static void ReadErrorMessage(IMessage message, string text, EngineIO eio)
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

        private static void ReadBinaryMessage(IMessage message, string text)
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

        private static void ReadBinaryAckMessage(IMessage message, string text)
        {
            var index1 = text.IndexOf('-');
            message.BinaryCount = int.Parse(text.Substring(0, index1));

            var index2 = text.IndexOf('[');

            var index3 = text.LastIndexOf(',', index2);
            if (index3 > -1)
            {
                message.Namespace = text.Substring(index1 + 1, index3 - index1 - 1);
                message.Id = int.Parse(text.Substring(index3 + 1, index2 - index3 - 1));
            }
            else
            {
                message.Id = int.Parse(text.Substring(index1 + 1, index2 - index1 - 1));
            }

            message.ReceivedText = text.Substring(index2);
        }

        #endregion
    }
}