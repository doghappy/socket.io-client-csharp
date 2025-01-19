namespace SocketIOClient.V2;

public interface IProtocolObservable
{
    void Subscribe(IProtocolObserver observer);
}