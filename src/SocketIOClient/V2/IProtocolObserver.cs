namespace SocketIOClient.V2;

public interface IProtocolObserver
{
    void OnNext(IProtocolMessage message);
}