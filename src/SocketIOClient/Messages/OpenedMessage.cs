using System;
using System.Collections.Generic;
using SocketIOClient.Transport;
using SocketIOClient.JsonSerializer;

namespace SocketIOClient.Messages
{
    public class OpenedMessage<T> : IMessage
    {
        public MessageType Type => MessageType.Opened;

        public string Sid { get; set; }

        public string Namespace { get; set; }

        public List<string> Upgrades { get; private set; }

        public int PingInterval { get; private set; }

        public int PingTimeout { get; private set; }

        public List<byte[]> OutgoingBytes { get; set; }

        public List<byte[]> IncomingBytes { get; set; }

        public int BinaryCount { get; }

        public EngineIO EIO { get; set; }

        public TransportProtocol Protocol { get; set; }
        public IJsonSerializer Serializer { get; set; }



        public void Read(string msg)
        {
            //var doc = JsonDocument.Parse(msg);
            //var root = doc.RootElement;
            var root = Serializer.GetRootElement<T>(msg);
            //Sid = root.GetProperty("sid").GetString();
            Sid = Serializer.GetString(Serializer.GetProperty<T>(root, "sid"));

            //PingInterval = GetInt32FromJsonElement(root, msg, "pingInterval");
            PingInterval = Serializer.GetInt32FromJsonElement(root, msg, "pingInterval");
            //PingTimeout = GetInt32FromJsonElement(root, msg, "pingTimeout");
            PingTimeout = Serializer.GetInt32FromJsonElement(root, msg, "pingTimeout");

            Upgrades = new List<string>();
            //var upgrades = root.GetProperty("upgrades").EnumerateArray();
            var upgrades = Serializer.GetListOfElements<T>(Serializer.GetProperty<T>(root, "upgrades"));
            foreach (var item in upgrades)
            {
                //Upgrades.Add(item.GetString());
                Upgrades.Add(Serializer.GetString(item));
            }
        }

        public string Write()
        {
            //var builder = new StringBuilder();
            //builder.Append("40");
            //if (!string.IsNullOrEmpty(Namespace))
            //{
            //    builder.Append(Namespace).Append(',');
            //}
            //return builder.ToString();
            throw new NotImplementedException();
        }
    }
}
