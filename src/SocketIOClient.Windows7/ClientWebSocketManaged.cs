using SocketIOClient.Transport;
using SocketIOClient.Transport.WebSockets;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Windows7
{
    public class SystemNetWebSocketsClientWebSocket : IClientWebSocket
    {
        public SystemNetWebSocketsClientWebSocket()
        {
            _ws = new System.Net.WebSockets.Managed.ClientWebSocket();
        }

        readonly System.Net.WebSockets.Managed.ClientWebSocket _ws;

        public Transport.WebSockets.WebSocketState State => (Transport.WebSockets.WebSocketState)_ws.State;

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            await _ws.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendAsync(byte[] bytes, TransportMessageType type, bool endOfMessage, CancellationToken cancellationToken)
        {
            var msgType = WebSocketMessageType.Text;
            if (type == TransportMessageType.Binary)
            {
                msgType = WebSocketMessageType.Binary;
            }
            await _ws.SendAsync(new ArraySegment<byte>(bytes), msgType, endOfMessage, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Transport.WebSockets.WebSocketReceiveResult> ReceiveAsync(int bufferSize, CancellationToken cancellationToken)
        {
            var buffer = new byte[bufferSize];
            var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
            return new Transport.WebSockets.WebSocketReceiveResult
            {
                Count = result.Count,
                MessageType = (TransportMessageType)result.MessageType,
                EndOfMessage = result.EndOfMessage,
                Buffer = buffer,
            };
        }

        public void AddHeader(string key, string val)
        {
            _ws.Options.SetRequestHeader(key, val);
        }

        public void SetProxy(IWebProxy proxy) => _ws.Options.Proxy = proxy;

        public void Dispose()
        {
            _ws.Dispose();
        }
    }
}
