using System;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core;

namespace SocketIOClient.Protocol.WebSocket;

public interface IWebSocketAdapter : IProtocolAdapter
{
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken);
}