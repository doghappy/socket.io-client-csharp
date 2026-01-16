using SocketIOClient.Session.EngineIOAdapter;

namespace SocketIOClient.Session.WebSocket.EngineIOAdapter;

public interface IWebSocketEngineIOAdapter : IEngineIOAdapter
{
    byte[] WriteProtocolFrame(byte[] bytes);
    byte[] ReadProtocolFrame(byte[] bytes);
}