using SocketIOClient.Transport;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Windows7
{
    public sealed class ClientWebSocketManaged : IClientWebSocket
    {
        public ClientWebSocketManaged()
        {
            _ws = new System.Net.WebSockets.Managed.ClientWebSocket();
        }

        readonly System.Net.WebSockets.Managed.ClientWebSocket _ws;

        public WebSocketState State => _ws.State;

        public async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            await _ws.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            await _ws.ConnectAsync(uri, cancellationToken);
        }

        public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return await _ws.ReceiveAsync(buffer, cancellationToken);
        }

        public async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            await _ws.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }

        public void Dispose()
        {
            _ws.Dispose();
        }
    }
}
