using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SocketIOClient.Transport;

namespace SocketIOClient.Messages
{
    public class OpenedMessage : IMessage
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

        public int Eio { get; set; }

        public TransportProtocol Protocol { get; set; }

        private int GetInt32FromJsonElement(JObject element, string msg, string name)
        {
            var p = element.Property(name);
            int val;
            switch (p.Value.Type)
            {
                case JTokenType.String:
                    val = int.Parse(p.Value.Value<string>());
                    break;
                case JTokenType.Integer:
                    val = p.Value.Value<int>();
                    break;
                default:
                    throw new ArgumentException($"Invalid message: '{msg}'");
            }
            return val;
        }

        public void Read(string msg)
        {
            var doc = JObject.Parse(msg);
            Sid = doc.Value<string>("sid");
            PingInterval = GetInt32FromJsonElement(doc, msg, "pingInterval");
            PingTimeout = GetInt32FromJsonElement(doc, msg, "pingTimeout");
            Upgrades = doc.Property("upgrades").Value.ToObject<List<string>>();
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
