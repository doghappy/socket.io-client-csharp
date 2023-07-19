using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MessagePack;
using MessagePack.Resolvers;
using SocketIO.Core;
using SocketIO.Serializer.Core;

namespace SocketIO.Serializer.MessagePack
{
    public class SocketIOMessagePackSerializer : ISerializer
    {
        private readonly EngineIO _eio;

        public SocketIOMessagePackSerializer(EngineIO eio) : this(MessagePackSerializerOptions.Standard, eio)
        {
        }

        public SocketIOMessagePackSerializer(MessagePackSerializerOptions options, EngineIO eio)
        {
            _eio = eio;
            _options = options ?? MessagePackSerializerOptions.Standard;
        }

        private readonly MessagePackSerializerOptions _options;

        private static object[] InsertEventToData(string eventName, object[] data)
        {
            var newData = new object[data.Length + 1];
            newData[0] = eventName;
            Array.Copy(data, 0, newData, 1, data.Length);
            return newData;
        }

        private bool HasByteArray(object data)
        {
            if (data is null)
                return false;

            var dataType = data.GetType();
            if (dataType.IsSimpleType())
                return false;

            if (data is byte[] bytes)
                return bytes.Length > 0;

            if (data is IEnumerable items)
            {
                foreach (var item in items)
                {
                    var flag = HasByteArray(item);
                    if (flag)
                        return true;
                }
            }

            var props = dataType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in props)
            {
                var value = prop.GetValue(data);
                if (value is null || prop.PropertyType.IsSimpleType())
                    continue;
                var flag = HasByteArray(value);
                if (flag)
                    return true;
            }

            return false;
        }

        private List<SerializedItem> InternalSerialize(string eventName, int packetId, string nsp, object[] data)
        {
            var items = new List<object>();
            var message = new PackMessage
            {
                RawType = PackMessageType.Event,
                Id = packetId,
                Namespace = GetNsp(nsp),
                Data = items
            };
            if (eventName is not null)
            {
                items.Add(eventName);
            }

            if (data is not null && data.Length > 0)
            {
                items.AddRange(data);
            }

            var serializedItem = new SerializedItem
            {
                Type = SerializedMessageType.Binary
            };
            if (_eio == EngineIO.V3)
            {
                serializedItem.Binary = MessagePackSerializer.Serialize(message, _options);
                if (HasByteArray(data))
                {
                    serializedItem.Binary[6] = 5;
                }
            }
            else
            {
                serializedItem.Binary = MessagePackSerializer.Serialize(message, _options);
            }

            // var test = MessagePackSerializer.Serialize(message, _options);
            // var text = "0x" + BitConverter.ToString(test).Replace("-", ", 0x");
            return new List<SerializedItem>
            {
                serializedItem
            };
        }

        public List<SerializedItem> Serialize(string eventName, int packetId, string nsp, object[] data)
        {
            return InternalSerialize(eventName, packetId, nsp, data);
        }

        public List<SerializedItem> Serialize(int packetId, string nsp, object[] data)
        {
            var items = new List<object>();
            var message = new PackMessage
            {
                RawType = PackMessageType.Ack,
                Id = packetId,
                Namespace = GetNsp(nsp),
                Data = items
            };
            if (data is not null && data.Length > 0)
            {
                items.AddRange(data);
            }

            var serializedItem = new SerializedItem
            {
                Type = SerializedMessageType.Binary
            };
            if (_eio == EngineIO.V3)
            {
                serializedItem.Binary = MessagePackSerializer.Serialize(message, _options);
                if (HasByteArray(data))
                {
                    serializedItem.Binary[6] = 6;
                }
            }
            else
            {
                serializedItem.Binary = MessagePackSerializer.Serialize(message, _options);
            }

            // var test = MessagePackSerializer.Serialize(message, _options);
            // var text = "0x" + BitConverter.ToString(test).Replace("-", ", 0x");
            return new List<SerializedItem>
            {
                serializedItem
            };
        }

        public List<SerializedItem> Serialize(string eventName, string nsp, object[] data)
        {
            return InternalSerialize(eventName, 0, nsp, data);
        }

        private static string GetNsp(string ns)
        {
            return string.IsNullOrEmpty(ns) ? "/" : ns;
        }

        public T Deserialize<T>(IMessage2 message, int index)
        {
            var packMessage = (PackMessage)message;
            var data = packMessage.DataList[index];
            var bytes = MessagePackSerializer.Serialize(data, _options);
            return MessagePackSerializer.Deserialize<T>(bytes, _options);
        }

        public object Deserialize(IMessage2 message, int index, Type returnType)
        {
            var packMessage = (PackMessage)message;
            var data = packMessage.DataList[index];
            var bytes = MessagePackSerializer.Serialize(data, _options);
            return MessagePackSerializer.Deserialize(returnType, bytes, _options);
        }

        public IMessage2 Deserialize(byte[] bytes)
        {
            var json = MessagePackSerializer.ConvertToJson(bytes, _options);
            var newBytes = MessagePackSerializer.ConvertFromJson(json);
            var odm = MessagePackSerializer.Deserialize<ObjectDataMessage>(newBytes, _options);
            var type = (MessageType)(40 + odm.Type);
            var message = new PackMessage(type);
            ReadMessage(message, odm, _eio);
            return message;
        }

        public IMessage2 Deserialize(string text)
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
                    return HandleDefaultMessage(protocol, text.Substring(1), _eio);
            }
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
            var data = ((PackMessage)message).DataList;
            return MessagePackSerializer.SerializeToJson(data, _options);
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

        public SerializedItem SerializeConnectedMessage(string ns, object auth,
            IEnumerable<KeyValuePair<string, string>> queries)
        {
            return _eio switch
            {
                EngineIO.V3 => SerializeEio3ConnectedMessage(ns, queries),
                EngineIO.V4 => SerializeEio4ConnectedMessage(ns, auth),
                _ => throw new ArgumentOutOfRangeException(nameof(EngineIO), _eio, null)
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

        private SerializedItem SerializeEio4ConnectedMessage(string nsp, object auth)
        {
            var message = new GenericMessage
            {
                Nsp = GetNsp(nsp)
            };
            if (auth is not null)
            {
                message.Data = auth;
            }
            else
            {
                message.Data = new
                {
                    buffer = new byte[1],
                    type = 0
                };
            }

            // var message = new PackMessage(MessageType.Connected)
            // {
            //     Namespace = GetNsp(nsp)
            // };
            // if (auth is not null)
            // {
            //     message.Data = auth;
            // }

            // var test = MessagePackSerializer.SerializeToJson(message);

            // var test = MessagePackSerializer.Serialize(message);
            // var text = "0x" + BitConverter.ToString(test).Replace("-", ", 0x");
            return new()
            {
                Type = SerializedMessageType.Binary,
                Binary = MessagePackSerializer.Serialize(message, _options)
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
                case MessageType.Ack:
                    ReadAckMessage(message, odm);
                    break;
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

        private static void ReadEventMessage(IMessage2 message, ObjectDataMessage odm)
        {
            message.Namespace = odm.Namespace;
            message.Id = odm.Id;

            var packMessage = (PackMessage)message;
            packMessage.Data = (object[])odm.Data;
        }

        private static void ReadAckMessage(IMessage2 message, ObjectDataMessage odm)
        {
            message.Namespace = odm.Namespace;
            message.Id = odm.Id;

            var data = (object[])odm.Data;
            var packMessage = (PackMessage)message;
            packMessage.Data = data.ToList();
        }

        private static void ReadErrorMessage(IMessage2 message, ObjectDataMessage odm)
        {
            var dictionary = (Dictionary<object, object>)odm.Data;
            message.Error = (string)dictionary["message"];
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

        #endregion
    }
}