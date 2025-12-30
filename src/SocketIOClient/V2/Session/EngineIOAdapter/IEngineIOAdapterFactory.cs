using SocketIOClient.Core;

namespace SocketIOClient.V2.Session.EngineIOAdapter;

public interface IEngineIOAdapterFactory
{
    T Create<T>(EngineIOCompatibility compatibility) where T : IEngineIOAdapter;
}