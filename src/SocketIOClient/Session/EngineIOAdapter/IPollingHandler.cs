using System.Threading.Tasks;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Session.EngineIOAdapter;

public interface IPollingHandler
{
    void StartPolling(OpenedMessage message, bool autoUpgrade);
    Task WaitHttpAdapterReady();
}