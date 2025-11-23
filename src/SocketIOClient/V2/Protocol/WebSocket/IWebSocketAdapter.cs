using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core;

namespace SocketIOClient.V2.Protocol.WebSocket;

public interface IWebSocketAdapter : IProtocolAdapter
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken);
}