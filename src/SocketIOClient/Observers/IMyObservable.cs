namespace SocketIOClient.Observers;

public interface IMyObservable<out T>
{
    void Subscribe(IMyObserver<T> observer);
}