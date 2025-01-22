using System.Collections.Generic;

namespace SocketIOClient.V2;

public interface ISerializer
{
    EngineIO EngineIO { get; set; }
    string Namespace { get; set; }
    IEnumerable<ProtocolMessage> Serialize(int packetId, object[] data);
}