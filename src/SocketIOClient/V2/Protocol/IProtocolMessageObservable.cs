namespace SocketIOClient.V2.Protocol;

public interface IProtocolMessageObservable
{
    void Subscribe(IProtocolMessageObserver observer);
}