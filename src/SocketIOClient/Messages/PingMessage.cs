using System.Collections.Generic;

namespace SocketIOClient.Messages
{
    public class PingMessage : IMessage
    {
        public MessageType Type => MessageType.Ping;

        public List<byte[]> OutgoingBytes { get; set; }

        public List<byte[]> IncomingBytes { get; set; }

        public int BinaryCount { get; }

        public void Read(string msg)
        {
        }

        public string Write() => "2";
    }
}
