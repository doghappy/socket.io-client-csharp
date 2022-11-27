using System.Collections.Generic;

namespace SocketIOClient.Messages
{
    public interface IBytesMessage : IMessage
    {

         List<byte[]> OutgoingBytes { get; }

         List<byte[]> IncomingBytes { get; }
    }
}