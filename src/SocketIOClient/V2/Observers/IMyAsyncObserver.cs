using System.Threading.Tasks;

namespace SocketIOClient.V2.Observers;

public interface IMyAsyncObserver<in T>
{
    Task OnNextAsync(T value);
}