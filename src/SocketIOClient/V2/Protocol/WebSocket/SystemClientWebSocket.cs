using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SocketIOClient.V2.Protocol.WebSocket;

public class SystemClientWebSocket : IWebSocketClient
{
    public SystemClientWebSocket(ILogger<SystemClientWebSocket> logger, WebSocketOptions options)
    {
        _ws = new ClientWebSocket();
        if (options.Proxy != null)
        {
            _ws.Options.Proxy = options.Proxy;
            logger.LogInformation("WebSocket proxy is enabled");
        }
    }

    private readonly ClientWebSocket _ws;

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

    public void SetDefaultHeader(string name, string value) => _ws.Options.SetRequestHeader(name, value);

    public void Dispose() => _ws.Dispose();
}