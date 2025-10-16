using SocketIOClient.Core;

namespace SocketIOClient.V2.Session.EngineIOHttpAdapter;

public interface IEngineIOAdapterFactory
{
    IEngineIOAdapter Create(EngineIO engineIO);
}