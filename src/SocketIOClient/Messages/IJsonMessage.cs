using System.Collections.Generic;

namespace SocketIOClient.Messages
{
    public interface IJsonMessage<T> : IMessage
    {
        List<T> JsonElements { get; }
    }
}