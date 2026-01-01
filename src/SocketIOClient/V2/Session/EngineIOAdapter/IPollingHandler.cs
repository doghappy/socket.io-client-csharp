using System.Threading.Tasks;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.V2.Session.EngineIOAdapter;

public interface IPollingHandler
{
    bool StartPolling(OpenedMessage message, bool autoUpgrade);
    Task WaitHttpAdapterReady();
}