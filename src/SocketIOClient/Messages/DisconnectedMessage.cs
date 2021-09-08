using System.Collections.Generic;

namespace SocketIOClient.Messages
{
    public class DisconnectedMessage : IMessage
    {
        public MessageType Type => MessageType.Disconnected;

        public string Namespace { get; set; }

        public ICollection<byte[]> OutgoingBytes { get; set; }

        public ICollection<byte[]> IncomingBytes { get; }

        public int BinaryCount { get; }

        public void Read(string msg)
        {
            Namespace = msg.TrimEnd(',');
        }

        public string Write()
        {
            if (string.IsNullOrEmpty(Namespace))
            {
                return "41";
            }
            return "41" + Namespace + ",";
        }
    }
}
