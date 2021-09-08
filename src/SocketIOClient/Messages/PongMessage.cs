using System.Collections.Generic;

namespace SocketIOClient.Messages
{
    public class PongMessage : IMessage
    {
        public MessageType Type => MessageType.Pong;

        public ICollection<byte[]> OutgoingBytes { get; set; }

        public ICollection<byte[]> IncomingBytes { get; }

        public int BinaryCount { get; }

        public void Read(string msg)
        {
        }

        public string Write() => "3";
    }
}
