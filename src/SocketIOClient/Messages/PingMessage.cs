using System.Collections.Generic;

namespace SocketIOClient.Messages
{
    public class PingMessage : IMessage
    {
        public MessageType Type => MessageType.Ping;

        public IEnumerable<byte[]> OutgoingBytes { get; set; }

        public void Read(string msg)
        {
        }

        public string Write() => "2";

        public string Eio3HttpWrite() => "1:2";
    }
}
