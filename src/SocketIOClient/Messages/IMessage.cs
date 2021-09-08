using System.Collections.Generic;

namespace SocketIOClient.Messages
{
    public interface IMessage
    {
        MessageType Type { get; }

        ICollection<byte[]> OutgoingBytes { get; set; }

        ICollection<byte[]> IncomingBytes { get; }

        int BinaryCount { get; }

        void Read(string msg);

        //void Eio3WsRead(string msg);

        //void Eio3HttpRead(string msg);

        string Write();

        //string Eio3WsWrite();
    }
}
