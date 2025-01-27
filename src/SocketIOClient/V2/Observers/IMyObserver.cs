namespace SocketIOClient.V2.Observers;

public interface IMyObserver<in T>
{
    void OnNext(T value);
}