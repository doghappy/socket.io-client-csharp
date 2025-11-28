using SocketIOClient.Core;

namespace SocketIOClient.V2.Session.Http.EngineIOHttpAdapter;

public interface IEngineIOAdapterFactory
{
    IEngineIOAdapter Create(EngineIO engineIO);
}