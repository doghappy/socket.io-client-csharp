namespace SocketIOClient.V2.Session;

public interface ISessionFactory
{
    ISession Create(SessionOptions options);
}