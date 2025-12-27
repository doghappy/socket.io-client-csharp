using System.Threading.Tasks;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.V2.Session.Http.EngineIOAdapter;

public interface IPollingHandler
{
    void OnOpenedMessageReceived(OpenedMessage message);
    Task WaitHttpAdapterReady();
}