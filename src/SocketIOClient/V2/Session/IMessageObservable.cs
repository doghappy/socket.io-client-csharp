namespace SocketIOClient.V2.Session;

public interface IMessageObservable
{
    void Subscribe(IMessageObserver observer);
}