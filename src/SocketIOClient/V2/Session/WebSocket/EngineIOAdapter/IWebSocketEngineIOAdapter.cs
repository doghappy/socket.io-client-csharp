using SocketIOClient.Core;
using SocketIOClient.V2.Session.EngineIOAdapter;

namespace SocketIOClient.V2.Session.WebSocket.EngineIOAdapter;

public interface IWebSocketEngineIOAdapter : IEngineIOAdapter
{
    void FormatBytesMessage(ProtocolMessage message);
}