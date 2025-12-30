using SocketIOClient.V2.Session.EngineIOAdapter;

namespace SocketIOClient.V2.Session.WebSocket.EngineIOAdapter;

public interface IWebSocketEngineIOAdapter : IEngineIOAdapter
{
    byte[] WriteProtocolFrame(byte[] bytes);
    byte[] ReadProtocolFrame(byte[] bytes);
}