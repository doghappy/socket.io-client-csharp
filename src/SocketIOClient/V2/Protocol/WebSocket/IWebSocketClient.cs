using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.WebSocket;

public interface IWebSocketClient
{
    Task SendAsync(IWebSocketMessage message, CancellationToken cancellationToken);
}