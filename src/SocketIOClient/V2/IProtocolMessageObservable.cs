namespace SocketIOClient.V2;

public interface IProtocolMessageObservable
{
    void Subscribe(IProtocolMessageObserver observer);
}