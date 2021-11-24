using SocketIOClient.Messages;
using SocketIOClient.Transport;
using SocketIOClient.UriConverters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Routers
{
    public class WebSocketRouter : Router
    {
        public WebSocketRouter(HttpClient httpClient, Func<IClientWebSocket> clientWebSocketProvider, SocketIOOptions options) : base(httpClient, clientWebSocketProvider, options)
        {
        }

        WebSocketTransport _webSocketTransport;
        IClientWebSocket _clientWebSocket;

        protected override TransportProtocol Protocol => TransportProtocol.WebSocket;

        public override async Task ConnectAsync()
        {
            await base.ConnectAsync();

            if (_webSocketTransport != null)
            {
                _webSocketTransport.Dispose();
            }
            Uri uri = UriConverter.GetServerUri(true, ServerUri, EIO, Options.Path, Options.Query);
            _clientWebSocket = ClientWebSocketProvider();
            if (Options.ExtraHeaders != null)
            {
                foreach (var item in Options.ExtraHeaders)
                {
                    _clientWebSocket.SetRequestHeader(item.Key, item.Value);
                }
            }
            _webSocketTransport = new WebSocketTransport(_clientWebSocket, EIO)
            {
                ConnectionTimeout = Options.ConnectionTimeout
            };
            _webSocketTransport.OnTextReceived = OnTextReceived;
            _webSocketTransport.OnBinaryReceived = OnBinaryReceived;
            _webSocketTransport.OnAborted = OnAborted;
            Debug.WriteLine($"[Websocket] Connecting");
            await _webSocketTransport.ConnectAsync(uri).ConfigureAwait(false);
            Debug.WriteLine($"[Websocket] Connected");
        }

        public override async Task DisconnectAsync()
        {
            try
            {
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            _clientWebSocket.Dispose();
            await base.DisconnectAsync();
        }

        protected override async Task SendAsync(string text, CancellationToken cancellationToken)
        {
            await _webSocketTransport.SendAsync(text, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task SendAsync(IEnumerable<byte[]> bytes, CancellationToken cancellationToken)
        {
            foreach (var item in bytes)
            {
                await _webSocketTransport.SendAsync(item, cancellationToken).ConfigureAwait(false);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_webSocketTransport != null)
            {
                _webSocketTransport.Dispose();
            }
        }
    }
}
