using System.Collections.Generic;
using SocketIOClient.V2.Core;
using SocketIOClient.V2.Message;
using SocketIOClient.V2.Protocol;

namespace SocketIOClient.V2.Serializer;

public interface ISerializer
{
    EngineIO EngineIO { get; set; }
    string Namespace { get; set; }
    List<ProtocolMessage> Serialize(object[] data);
    IMessage Deserialize(string text);
}