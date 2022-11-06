using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport.WebSockets
{
    public class DefaultClientWebSocket : IClientWebSocket
    {
        public DefaultClientWebSocket()
        {
            _ws = new ClientWebSocket();
        }

        readonly ClientWebSocket _ws;

        public WebSocketState State => (WebSocketState)_ws.State;

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

        public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var result = await _ws.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            return new WebSocketReceiveResult
            {
                Count = result.Count,
                MessageType = (TransportMessageType)result.MessageType,
                EndOfMessage = result.EndOfMessage
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
