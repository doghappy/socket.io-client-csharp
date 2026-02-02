using System.Threading.Tasks;
using SocketIOClient.Common.Messages;

namespace SocketIOClient.Session.EngineIOAdapter;

public interface IPollingHandler
{
    void StartPolling(OpenedMessage message, bool autoUpgrade);
    Task WaitHttpAdapterReady();
}