using System.Threading.Tasks;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.V2.Session.EngineIOAdapter;

public interface IPollingHandler
{
    void OnOpenedMessageReceived(OpenedMessage message);
    Task WaitHttpAdapterReady();
}