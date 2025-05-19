using System.Collections.Generic;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer
{
    public interface ISerializer
    {
        List<ProtocolMessage> Serialize(object[] data);
        List<ProtocolMessage> Serialize(object[] data, int packetId);
        IMessage Deserialize(string text);
        ProtocolMessage NewPingMessage();
        ProtocolMessage NewPongMessage();
    }
}