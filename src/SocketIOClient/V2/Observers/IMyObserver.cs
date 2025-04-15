using System.Threading.Tasks;

namespace SocketIOClient.V2.Observers;

public interface IMyObserver<in T>
{
    Task OnNextAsync(T protocolMessage);
}