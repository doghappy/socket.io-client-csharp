using System.Collections.Generic;

namespace SocketIOClient.Messages
{
    public class PingMessage : IMessage
    {
        public MessageType Type => MessageType.Ping;

        public ICollection<byte[]> OutgoingBytes { get; set; }

        public ICollection<byte[]> IncomingBytes { get; }

        public int BinaryCount { get; }

        public void Read(string msg)
        {
        }

        public string Write() => "2";
    }
}
