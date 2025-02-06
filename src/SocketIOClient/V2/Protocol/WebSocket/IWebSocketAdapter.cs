using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.WebSocket;

public interface IWebSocketAdapter : IProtocolAdapter
{
    Task ConnectAsync(CancellationToken cancellationToken);
}