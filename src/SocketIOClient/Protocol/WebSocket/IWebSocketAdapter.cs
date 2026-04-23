using System;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Common;

namespace SocketIOClient.Protocol.WebSocket;

public interface IWebSocketAdapter : IProtocolAdapter
{
    bool HasListenerStarted { get; }
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
}