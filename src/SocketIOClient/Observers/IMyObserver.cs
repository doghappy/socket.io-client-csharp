using System.Threading.Tasks;

namespace SocketIOClient.Observers;

public interface IMyObserver<in T>
{
    Task OnNextAsync(T message);
}