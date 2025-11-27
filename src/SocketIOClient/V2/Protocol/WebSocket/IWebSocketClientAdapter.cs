using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.WebSocket;

public interface IWebSocketClientAdapter
{
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task SendAsync(byte[] data, WebSocketMessageType messageType, CancellationToken cancellationToken);
}