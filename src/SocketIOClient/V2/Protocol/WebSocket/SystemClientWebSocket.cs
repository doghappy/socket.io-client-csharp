using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.WebSocket;

public class SystemClientWebSocket : IWebSocketClient
{
    private readonly ClientWebSocket _ws = new();

    public WebSocketState State => _ws.State;

    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) =>
        _ws.ConnectAsync(uri, cancellationToken);

    public Task SendAsync(ArraySegment<byte> buffer, System.Net.WebSockets.WebSocketMessageType messageType,
        bool endOfMessage,
        CancellationToken cancellationToken) =>
        _ws.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

    public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
        _ws.ReceiveAsync(buffer, cancellationToken);

    public Task CloseAsync(WebSocketCloseStatus closeStatus, string desc, CancellationToken cancellationToken) =>
        _ws.CloseAsync(closeStatus, desc, cancellationToken);

    public void Dispose() => _ws.Dispose();
}