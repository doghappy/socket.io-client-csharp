using System;
using System.Collections.Generic;

namespace SocketIO.Core
{
    public class Message
    {
        public Message(MessageType type)
        {
            Type = type;
        }

        public MessageType Type { get; }
        public string Sid { get; set; }
        public int PingInterval { get; set; }
        public int PingTimeout { get; set; }
        public List<string> Upgrades { get; set; }
        public int BinaryCount { get; set; }
        public List<byte[]> OutgoingBytes { get; set; }
        [Obsolete]
        public List<byte[]> IncomingBytes { get; set; }
        public string Namespace { get; set; }
        public TimeSpan Duration { get; set; }
        public int Id { get; set; }

        [Obsolete]
        public string Event { get; set; }
        public string Error { get; set; }
        public string ReceivedText { get; set; }
        public List<byte[]> ReceivedBinary { get; set; }
    }
}