using SocketIOClient.Core;

namespace SocketIOClient.Serializer;

public interface ISerializerFactory
{
    ISerializer Create(EngineIO engineIO);
}
