using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Protocol.WebSocket;

public interface IWebSocketClientAdapter
{
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task SendAsync(byte[] data, WebSocketMessageType messageType, CancellationToken cancellationToken);
    Task<WebSocketMessage> ReceiveAsync(CancellationToken cancellationToken);
    void SetDefaultHeader(string name, string value);
}