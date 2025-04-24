using System.Collections.Generic;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer
{
    public interface ISerializer
    {
        string Namespace { get; set; }
        List<ProtocolMessage> Serialize(object[] data);
        IMessage Deserialize(string text);
        ProtocolMessage NewPingMessage();
    }
}