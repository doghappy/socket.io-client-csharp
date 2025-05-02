using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MessagePack;
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

            if (dataType.IsArray)
            {
                if (data is byte[] bytes)
                    return bytes.Length > 0;

                foreach (var item in (IEnumerable)data)
                {
                    var flag = HasByteArray(item);
                    if (flag)
                        return true;
                }
            }
            else
            {
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
            }

            return false;
        }

        private List<SerializedItem> InternalSerialize(EngineIO eio, string eventName, int packetId, string nsp, object[] data)
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
            if (eio == EngineIO.V3)
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

        public List<SerializedItem> Serialize(EngineIO eio, string eventName, int packetId, string nsp, object[] data)
        {
            return InternalSerialize(eio, eventName, packetId, nsp, data);
        }

        public List<SerializedItem> Serialize(EngineIO eio, int packetId, string nsp, object[] data)
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
            if (eio == EngineIO.V3)
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

        public List<SerializedItem> Serialize(EngineIO eio, string eventName, string nsp, object[] data)
        {
            return InternalSerialize(eio, eventName, 0, nsp, data);
        }

        private static string GetNsp(string ns)
        {
            return string.IsNullOrEmpty(ns) ? "/" : ns;
        }

        public T Deserialize<T>(IMessage message, int index)
        {
            var data = Deserialize(message, index, typeof(T));
            return (T)data;
        }

        public object Deserialize(IMessage message, int index, Type returnType)
        {
            var packMessage = (PackMessage)message;
            var obj = packMessage.DataList[index];
            if (obj is null)
                return default;
            if (obj.GetType() == returnType)
                return obj;
            var data = (Dictionary<object, object>)obj;
            return data.ToObject(returnType);
        }

        public IMessage Deserialize(EngineIO eio, byte[] bytes)
        {
            var json = MessagePackSerializer.ConvertToJson(bytes, _options);
            var newBytes = MessagePackSerializer.ConvertFromJson(json);
            var odm = MessagePackSerializer.Deserialize<ObjectDataMessage>(newBytes, _options);
            var type = (MessageType)(40 + odm.Type);
            var message = new PackMessage(type);
            ReadMessage(message, odm, eio);
            return message;
        }

        public IMessage Deserialize(EngineIO eio, string text)
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
        }

        private static IMessage HandleDefaultMessage(EngineIOProtocol protocol, string text, EngineIO eio)
        {
            var bytes = MessagePackSerializer.ConvertFromJson(text);
            var odm = MessagePackSerializer.Deserialize<ObjectDataMessage>(bytes);
            var type = (MessageType)((int)protocol * 10 + odm.Type);
            var message = new PackMessage(type);
            ReadMessage(message, odm, eio);
            return message;
        }

        public string MessageToJson(IMessage message)
        {
            var data = ((PackMessage)message).DataList;
            return MessagePackSerializer.SerializeToJson(data, _options);
        }

        public IMessage NewMessage(MessageType type)
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

        public SerializedItem SerializePingProbeMessage()
        {
            return new SerializedItem
            {
                Text = "2probe"
            };
        }

        public SerializedItem SerializePongMessage()
        {
            return new SerializedItem
            {
                Text = "3"
            };
        }

        public SerializedItem SerializeUpgradeMessage()
        {
            return new SerializedItem
            {
                Text = "5"
            };
        }

        #region Serialize ConnectedMessage

        public SerializedItem SerializeConnectedMessage(EngineIO eio,
            string ns,
            object auth,
            IEnumerable<KeyValuePair<string, string>> queries)
        {
            return eio switch
            {
                EngineIO.V3 => SerializeEio3ConnectedMessage(ns, queries),
                EngineIO.V4 => SerializeEio4ConnectedMessage(ns, auth),
                _ => throw new ArgumentOutOfRangeException(nameof(EngineIO), eio, null)
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

            // var test = MessagePackSerializer.Serialize(message, _options);
            // var text = "0x" + BitConverter.ToString(test).Replace("-", ", 0x");
            return new()
            {
                Type = SerializedMessageType.Binary,
                Binary = MessagePackSerializer.Serialize(message, _options)
            };
        }

        #endregion

        #region Read Message

        private static void ReadMessage(IMessage message, ObjectDataMessage odm, EngineIO eio)
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
                case MessageType.Ack:
                case MessageType.Binary:
                case MessageType.BinaryAck:
                    ReadEventMessage(message, odm);
                    break;
                case MessageType.Error:
                    ReadErrorMessage(message, odm, eio);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message.Type), message.Type, null);
            }
        }

        private static void ReadOpenedMessage(IMessage message, ObjectDataMessage odm)
        {
            message.Sid = odm.Sid;
            message.PingInterval = odm.PingInterval;
            message.PingTimeout = odm.PingTimeout;
            message.Upgrades = odm.Upgrades;
        }

        private static void ReadConnectedMessage(IMessage message, ObjectDataMessage odm)
        {
            message.Namespace = odm.Namespace;
            if (odm.Data is not null)
            {
                var dictionary = (Dictionary<object, object>)odm.Data;
                message.Sid = dictionary["sid"].ToString();
            }
        }

        private static void ReadDisconnectedMessage(IMessage message, ObjectDataMessage odm)
        {
            message.Namespace = odm.Namespace;
        }

        private static void ReadEventMessage(IMessage message, ObjectDataMessage odm)
        {
            message.Namespace = odm.Namespace;
            message.Id = odm.Id;

            var packMessage = (PackMessage)message;
            packMessage.Data = (object[])odm.Data;
        }

        private static void ReadErrorMessage(IMessage message, ObjectDataMessage odm, EngineIO eio)
        {
            message.Namespace = odm.Namespace;
            if (eio == EngineIO.V3)
            {
                message.Error = (string)odm.Data;
            }
            else
            {
                var dictionary = (Dictionary<object, object>)odm.Data;
                message.Error = (string)dictionary["message"];
            }
        }

        #endregion
    }
}