using SocketIOClient.Core;

namespace SocketIOClient.Serializer;

public interface IEngineIOMessageAdapterFactory
{
    IEngineIOMessageAdapter Create(EngineIO engineIO);
}