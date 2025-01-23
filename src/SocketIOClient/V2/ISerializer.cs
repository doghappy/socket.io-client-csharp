using System.Collections.Generic;
using SocketIOClient.V2.Protocol;

namespace SocketIOClient.V2;

public interface ISerializer
{
    EngineIO EngineIO { get; set; }
    string Namespace { get; set; }
    IEnumerable<ProtocolMessage> Serialize(int packetId, object[] data);
}