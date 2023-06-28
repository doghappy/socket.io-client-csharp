using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using SocketIO.Core;
using SocketIO.Serializer.Core;

namespace SocketIO.Serializer.MessagePack
{
    public class SocketIOMessagePackSerializer : ISerializer
    {
        public SocketIOMessagePackSerializer() : this(MessagePackSerializerOptions.Standard)
        {
        }

        public SocketIOMessagePackSerializer(MessagePackSerializerOptions options)
        {
            _options = options ?? MessagePackSerializerOptions.Standard;
        }

        private readonly MessagePackSerializerOptions _options;
        // private int _id;

        // private MessagePackSerializerOptions NewOptions(JsonConverter converter)
        // {
        //     
        //     var options = new MessagePackSerializerOptions(convert);
        //     options.Converters.Add(converter);
        //     return options;
        // }

        public string Serialize(object data)
        {
            var bytes = MessagePackSerializer.Serialize(data, _options);
            return MessagePackSerializer.ConvertToJson(bytes, _options);
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
            // return InternalSerialize(eventName, packetId, ns, data);
            throw new NotImplementedException();
        }

        public List<SerializedItem> Serialize(int packetId, string ns, object[] data)
        {
            var message = new PackMessage2
            {
                Id = packetId,
                Nsp = ns,
            };
            if (data is not null && data.Length > 0)
            {
                message.Data.AddRange(data);
            }

            return new List<SerializedItem>
            {
                new()
                {
                    Type = SerializedMessageType.Binary,
                    Binary = MessagePackSerializer.Serialize(message)
                }
            };
        }

        public List<SerializedItem> Serialize(string eventName, string ns, object[] data)
        {
            var message = new PackMessage2
            {
                Type = PackMessageType.Event,
                Data = new List<object>(1 + data.Length)
                {
                    eventName
                },
            };
            message.Data.AddRange(data);
            message.Nsp = GetNsp(ns);
            return new List<SerializedItem>
            {
                new()
                {
                    Type = SerializedMessageType.Binary,
                    Binary = MessagePackSerializer.Serialize(message)
                }
            };
        }

        private static string GetNsp(string ns)
        {
            return string.IsNullOrEmpty(ns) ? "/" : ns;
        }

        // private List<SerializedItem> InternalSerialize(string eventName, int? packetId, string ns, object[] data)
        // {
        //     var newData = InsertEventToData(eventName, data);
        //
        //     var converter = new ByteArrayConverter();
        //     var options = NewOptions(converter);
        //     var json = JsonSerializer.Serialize(newData, options);
        //
        //     var builder = new StringBuilder();
        //     if (converter.Bytes.Count == 0)
        //     {
        //         builder.Append("42");
        //     }
        //     else
        //     {
        //         builder.Append("45").Append(converter.Bytes.Count).Append('-');
        //     }
        //
        //     if (!string.IsNullOrEmpty(ns))
        //     {
        //         builder.Append(ns).Append(',');
        //     }
        //
        //     if (packetId is not null)
        //     {
        //         builder.Append(packetId);
        //     }
        //
        //     builder.Append(json);
        //     return NewSerializedItems(builder, converter.Bytes);
        // }
        //
        // private (JsonNode jsonNode, JsonSerializerOptions options) GetSerializationData(IMessage2 message, int index)
        // {
        //     var jsonMessage = (JsonMessage)message;
        //     var item = jsonMessage.JsonArray[index];
        //     var converter = new ByteArrayConverter();
        //     if (jsonMessage.ReceivedBinary is not null)
        //     {
        //         converter.Bytes.AddRange(jsonMessage.ReceivedBinary);
        //     }
        //
        //     var options = NewOptions(converter);
        //     return (item, options);
        // }

        public T Deserialize<T>(IMessage2 message, int index)
        {
            // var (jsonNode, options) = GetSerializationData(message, index);
            // return jsonNode.Deserialize<T>(options);
            throw new NotImplementedException();
        }

        public object Deserialize(IMessage2 message, int index, Type returnType)
        {
            // var (jsonNode, options) = GetSerializationData(message, index);
            // return jsonNode.Deserialize(returnType, options);
            throw new NotImplementedException();
        }

        public IMessage2 Deserialize(EngineIO eio, byte[] bytes)
        {
            var odm = MessagePackSerializer.Deserialize<ObjectDataMessage>(bytes);
            var type = (MessageType)(40 + odm.Type);
            var message = new PackMessage(type);
            ReadMessage(message, odm, eio);
            return message;
        }

        public IMessage2 Deserialize(EngineIO eio, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            if (!int.TryParse(text.Substring(0, 1), out var intProtocol))
            {
                return null;
            }

            if (!Enum.IsDefined(typeof(EngineIOProtocol), intProtocol))
            {
                return null;
            }

            var protocol = (EngineIOProtocol)intProtocol;
            switch (protocol)
            {
                case EngineIOProtocol.Ping:
                    return NewMessage(MessageType.Ping);
                case EngineIOProtocol.Pong:
                    return NewMessage(MessageType.Pong);
                // case EngineIOProtocol.Message:
                //     return HandleEventMessage();
                default:
                    return HandleDefaultMessage(protocol, text.Substring(1), eio);
            }
            // switch (protocol)
            // {
            //     case '4':
            //         return HandleEventMessage();
            //     default:
            //         return HandleEngineProtocol(text);
            // }

            // var enums = Enum.GetValues(typeof(MessageType));
            // foreach (MessageType type in enums)
            // {
            //     var prefix = ((int)type).ToString();
            //     if (!text.StartsWith(prefix)) continue;
            //
            //     var message = NewMessage(type);
            //     ReadMessage(message, eio, text.Substring(prefix.Length));
            //     return message;
            // }
            //
            // return null;
        }

        private static IMessage2 HandleEventMessage()
        {
            return null;
        }

        private static IMessage2 HandleDefaultMessage(EngineIOProtocol protocol, string text, EngineIO eio)
        {
            var bytes = MessagePackSerializer.ConvertFromJson(text);
            var odm = MessagePackSerializer.Deserialize<ObjectDataMessage>(bytes);
            var type = (MessageType)((int)protocol * 10 + odm.Type);
            var message = new PackMessage(type);
            ReadMessage(message, odm, eio);
            return message;
        }

        public string MessageToJson(IMessage2 message)
        {
            // return ((JsonMessage)message).JsonArray.ToJsonString(_options);
            throw new NotImplementedException();
        }

        public IMessage2 NewMessage(MessageType type)
        {
            return new PackMessage(type);
        }

        public SerializedItem SerializePingMessage()
        {
            return new SerializedItem
            {
                Text = "2"
            };
        }

        public SerializedItem SerializePongMessage()
        {
            return new SerializedItem
            {
                Text = "3"
            };
        }

        #region Serialize ConnectedMessage

        public SerializedItem SerializeConnectedMessage(
            string ns,
            EngineIO eio,
            object auth,
            IEnumerable<KeyValuePair<string, string>> queries)
        {
            return eio switch
            {
                EngineIO.V3 => SerializeEio3ConnectedMessage(ns, queries),
                EngineIO.V4 => SerializeEio4ConnectedMessage(ns, auth),
                _ => throw new ArgumentOutOfRangeException(nameof(eio), eio, null)
            };
        }

        private static SerializedItem SerializeEio3ConnectedMessage(
            string nsp,
            IEnumerable<KeyValuePair<string, string>> queries)
        {
            if (string.IsNullOrEmpty(nsp))
            {
                return null;
            }

            var serializedItem = new SerializedItem
            {
                Type = SerializedMessageType.Binary
            };

            if (queries is null)
            {
                serializedItem.Binary = MessagePackSerializer.Serialize(new
                {
                    type = PackMessageType.Connected,
                    nsp
                });
                return serializedItem;
            }

            var builder = new StringBuilder();
            var i = -1;
            foreach (var item in queries)
            {
                i++;
                if (i > 0)
                    builder.Append('&');
                builder.Append(item.Key).Append('=').Append(item.Value);
            }

            var query = builder.ToString();
            serializedItem.Binary = MessagePackSerializer.Serialize(new
            {
                type = PackMessageType.Connected,
                query,
                nsp = nsp + "?" + query
            });
            return serializedItem;
        }

        private static SerializedItem SerializeEio4ConnectedMessage(string nsp, object auth)
        {
            var message = new GenericMessage
            {
                Nsp = GetNsp(nsp)
            };
            if (auth is not null)
            {
                message.Data = auth;
            }

            return new()
            {
                Type = SerializedMessageType.Binary,
                Binary = MessagePackSerializer.Serialize(message)
            };
        }

        #endregion

        #region Read Message

        private static void ReadMessage(IMessage2 message, ObjectDataMessage odm, EngineIO eio)
        {
            switch (message.Type)
            {
                case MessageType.Opened:
                    ReadOpenedMessage(message, odm);
                    break;
                // case MessageType.Ping:
                //     break;
                // case MessageType.Pong:
                //     break;
                case MessageType.Connected:
                    ReadConnectedMessage(message, odm);
                    break;
                case MessageType.Disconnected:
                    ReadDisconnectedMessage(message, odm);
                    break;
                case MessageType.Event:
                    ReadEventMessage(message, odm);
                    break;
                // case MessageType.Ack:
                //     ReadAckMessage(message, text);
                //     break;
                case MessageType.Error:
                    ReadErrorMessage(message, odm);
                    break;
                // case MessageType.Binary:
                //     ReadBinaryMessage(message, text);
                //     break;
                // case MessageType.BinaryAck:
                //     ReadBinaryAckMessage(message, text);
                //     break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message.Type), message.Type, null);
            }
        }

        private static void ReadOpenedMessage(IMessage2 message, ObjectDataMessage odm)
        {
            message.Sid = odm.Sid;
            message.PingInterval = odm.PingInterval;
            message.PingTimeout = odm.PingTimeout;
            message.Upgrades = odm.Upgrades;
        }

        private static void ReadConnectedMessage(IMessage2 message, ObjectDataMessage odm)
        {
            // switch (eio)
            // {
            //     case EngineIO.V3:
            //         ReadEio3ConnectedMessage(message, odm);
            //         break;
            //     // case EngineIO.V4:
            //     //     ReadEio4ConnectedMessage(message, objectData);
            //     //     break;
            //     default:
            //         throw new ArgumentOutOfRangeException(nameof(eio), eio, null);
            // }
            message.Namespace = odm.Namespace;
            if (odm.Data is not null)
            {
                var dictionary = (Dictionary<object, object>)odm.Data;
                message.Sid = dictionary["sid"].ToString();
            }
        }

        private static void ReadDisconnectedMessage(IMessage2 message, ObjectDataMessage odm)
        {
            message.Namespace = odm.Namespace;
        }

        private static void ReadEio4ConnectedMessage(IMessage2 message, string text)
        {
            // var index = text.IndexOf('{');
            // if (index > 0)
            // {
            //     message.Namespace = text.Substring(0, index - 1);
            //     text = text.Substring(index);
            // }
            // else
            // {
            //     message.Namespace = string.Empty;
            // }
            //
            // message.Sid = JsonDocument.Parse(text).RootElement.GetProperty("sid").GetString();
            throw new NotImplementedException();
        }

        private static void ReadEio3ConnectedMessage(IMessage2 message, ObjectDataMessage odm)
        {
            message.Namespace = odm.Namespace;
        }

        private static void ReadEventMessage(IMessage2 message, ObjectDataMessage odm)
        {
            message.Namespace = odm.Namespace;
            message.Id = odm.Id;

            var data = (object[])odm.Data;
            var packMessage = (PackMessage)message;
            packMessage.Event = (string)data[0];
            packMessage.Data = data.Skip(1).ToList();
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

        private static void ReadErrorMessage(IMessage2 message, ObjectDataMessage odm)
        {
            message.Error = (string)odm.Data;
            message.Namespace = odm.Namespace;
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

        private static void ReadBinaryAckMessage(IMessage2 message, string text)
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