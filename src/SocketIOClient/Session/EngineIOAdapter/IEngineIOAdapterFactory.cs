using SocketIOClient.Core;

namespace SocketIOClient.Session.EngineIOAdapter;

public interface IEngineIOAdapterFactory
{
    T Create<T>(EngineIOCompatibility compatibility) where T : IEngineIOAdapter;
}