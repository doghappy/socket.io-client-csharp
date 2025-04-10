using SocketIOClient.V2.Session;

namespace SocketIOClient.V2;

public interface ISessionFactory
{
    ISession New(EngineIO eio);
}