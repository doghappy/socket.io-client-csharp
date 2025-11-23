using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.WebSocket;

public interface IWebSocketClient : IDisposable
{
    WebSocketState State { get; }
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task SendAsync(ArraySegment<byte> buffer, System.Net.WebSockets.WebSocketMessageType messageType, bool endOfMessage,
        CancellationToken cancellationToken);
    Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);
    Task CloseAsync(WebSocketCloseStatus closeStatus, string desc, CancellationToken cancellationToken);
}