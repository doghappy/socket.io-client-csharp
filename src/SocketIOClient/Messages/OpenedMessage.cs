using System;
using System.Text.Json;
using System.Collections.Generic;
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

        public void Read(string msg)
        {
            var doc = JsonDocument.Parse(msg);
            var root = doc.RootElement;
            Sid = root.GetProperty("sid").GetString();
            PingInterval = root.GetProperty("pingInterval").GetInt32();
            PingTimeout = root.GetProperty("pingTimeout").GetInt32();
            Upgrades = new List<string>();
            var upgrades = root.GetProperty("upgrades").EnumerateArray();
            foreach (var item in upgrades)
            {
                Upgrades.Add(item.GetString());
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
