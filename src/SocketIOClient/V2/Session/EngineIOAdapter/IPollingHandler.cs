using System.Threading.Tasks;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.V2.Session.EngineIOAdapter;

public interface IPollingHandler
{
    void StartPolling(OpenedMessage message);
    Task WaitHttpAdapterReady();
}