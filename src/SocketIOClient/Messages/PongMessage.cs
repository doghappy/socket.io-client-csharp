using System.Collections.Generic;

namespace SocketIOClient.Messages
{
    public class PongMessage : IMessage
    {
        public MessageType Type => MessageType.Pong;

        public List<byte[]> OutgoingBytes { get; set; }

        public List<byte[]> IncomingBytes { get; set; }

        public int BinaryCount { get; }

        public void Read(string msg)
        {
        }

        public string Write() => "3";
    }
}
