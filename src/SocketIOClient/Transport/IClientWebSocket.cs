using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport
{
    public interface IClientWebSocket : IDisposable
    {
        WebSocketState State { get; }
        Action<object> ConfigOptions { get; set; }
        Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
        Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);
        Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
        Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);
        void SetRequestHeader(string headerName, string headerValue);
    }
}
