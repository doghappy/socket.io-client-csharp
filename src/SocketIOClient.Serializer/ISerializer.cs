using System.Collections.Generic;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer
{
    public interface ISerializer
    {
        string Namespace { get; set; }
        List<ProtocolMessage> Serialize(object[] data);
        List<ProtocolMessage> Serialize(object[] data, int packetId);
        List<ProtocolMessage> SerializeAckData(object[] data, int packetId);
        IMessage Deserialize(string text);
        void SetEngineIOMessageAdapter(IEngineIOMessageAdapter adapter);
    }
}