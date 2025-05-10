namespace SocketIOClient.V2.Observers;

public interface IMyObservable<out T>
{
    void Subscribe(IMyObserver<T> observer);
}