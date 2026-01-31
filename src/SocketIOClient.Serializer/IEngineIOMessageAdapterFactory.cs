using SocketIOClient.Common;

namespace SocketIOClient.Serializer;

public interface IEngineIOMessageAdapterFactory
{
    IEngineIOMessageAdapter Create(EngineIO engineIO);
}