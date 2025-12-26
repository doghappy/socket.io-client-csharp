using SocketIOClient.Core;

namespace SocketIOClient.V2.Session.EngineIOAdapter;

public interface IEngineIOAdapterFactory
{
    IEngineIOAdapter Create(EngineIOCompatibility compatibility);
}