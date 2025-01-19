namespace SocketIOClient.V2;

public interface IMessageObservable
{
    void Subscribe(IMessageObserver observer);
}