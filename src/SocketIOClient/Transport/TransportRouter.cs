using SocketIOClient.Messages;
using SocketIOClient.UriConverters;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport
{
    public class TransportRouter : IDisposable
    {
        public TransportRouter(HttpClient httpClient, IClientWebSocket clientWebSocket)
        {
            _httpClient = httpClient;
            _clientWebSocket = clientWebSocket;
            UriConverter = new UriConverter();
            Eio = 4;
            Path = "/socket.io";
        }

        readonly HttpClient _httpClient;
        readonly IClientWebSocket _clientWebSocket;

        HttpTransport _httpTransport;
        WebSocketTransport _webSocketTransport;
        string _httpUri;

        public Uri ServerUri { get; set; }

        public int Eio { get; set; }

        public string Path { get; set; }

        public string Namespace { get; set; }

        public IEnumerable<KeyValuePair<string, string>> QueryParams { get; set; }

        public TransportProtocol Protocol { get; private set; }

        public string Sid { get; private set; }

        public IUriConverter UriConverter { get; set; }

        public Action<IMessage> OnMessageReceived { get; set; }

        public async Task ConnectAsync()
        {
            if (_webSocketTransport != null)
            {
                _webSocketTransport.Dispose();
            }

            Uri uri = UriConverter.GetHandshakeUri(ServerUri, Eio, Path, QueryParams);
            string text = await _httpClient.GetStringAsync(uri).ConfigureAwait(false);

            int index = text.IndexOf('{');
            string json = text.Substring(index);
            var info = System.Text.Json.JsonSerializer.Deserialize<HandshakeInfo>(json);
            Sid = info.Sid;
            if (info.Upgrades.Contains("websocket"))
            {
                _webSocketTransport = new WebSocketTransport(_clientWebSocket);
                await WebSocketConnectAsync().ConfigureAwait(false);
                Protocol = TransportProtocol.WebSocket;
            }
            else
            {
                _httpUri = uri + "&sid=" + Sid;
                _httpTransport = new HttpTransport(_httpClient);
                await HttpConnectAsync().ConfigureAwait(false);
                Protocol = TransportProtocol.Polling;
            }
        }

        private async Task WebSocketConnectAsync()
        {
            Uri uri = UriConverter.GetWebSocketUri(ServerUri, Eio, Path, QueryParams, Sid);
            await _webSocketTransport.ConnectAsync(uri).ConfigureAwait(false);
            _webSocketTransport.OnTextReceived = OnWebSocketTextReceived;
            _webSocketTransport.OnBinaryReceived = OnBinaryReceived;
            await _webSocketTransport.SendAsync("2probe", CancellationToken.None);
        }

        private async Task HttpConnectAsync()
        {
            _httpTransport.OnTextReceived = OnTextReceived;
            _httpTransport.OnBinaryReceived = OnBinaryReceived;
            if (!string.IsNullOrEmpty(Namespace))
            {
                var builder = new StringBuilder();
                builder.Append("40").Append(Namespace);
                if (QueryParams != null)
                {
                    int i = -1;
                    foreach (var item in QueryParams)
                    {
                        i++;
                        if (i == 0)
                        {
                            builder.Append('?');
                        }
                        else
                        {
                            builder.Append('&');
                        }
                        builder.Append(item.Key).Append('=').Append(item.Value);
                    }
                }
                builder.Append(',');
                builder.Insert(0, builder.Length + ":");
                await _httpTransport.PostAsync(_httpUri, builder.ToString(), CancellationToken.None);
            }
            _ = Task.Factory.StartNew(HttpPolling, TaskCreationOptions.LongRunning);
        }

        private async Task HttpPolling()
        {
            while (true)
            {
                await _httpTransport.GetAsync(_httpUri, CancellationToken.None);
            }
        }

        private void OnWebSocketTextReceived(string text)
        {
            if (text == "3probe")
            {
                _ = _webSocketTransport.SendAsync("5", CancellationToken.None);
            }
            else
            {
                OnTextReceived(text);
            }
        }

        private void OnTextReceived(string text)
        {
            IMessage msg = null;
            if (Eio == 3)
            {
                if (Protocol == TransportProtocol.Polling)
                {
                    msg = MessageFactory.GetEio3HttpMessage(text);
                }
                else if (Protocol == TransportProtocol.WebSocket)
                {
                    msg = MessageFactory.GetEio3WebSocketMessage(text);
                }
            }
            else if (Eio == 4)
            {
                msg = MessageFactory.GetEio4Message(text);
            }

            if (msg != null)
            {
                OnMessageReceived(msg);
            }
        }

        private void OnBinaryReceived(byte[] bytes)
        {

        }

        public async Task SendAsync(string text, CancellationToken cancellationToken)
        {
            if (Protocol == TransportProtocol.Polling)
            {
                await _httpTransport.PostAsync(_httpUri, text, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _webSocketTransport.SendAsync(text, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SendAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            if (Protocol == TransportProtocol.Polling)
            {
                await _httpTransport.PostAsync(_httpUri, bytes, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _webSocketTransport.SendAsync(bytes, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            if (_webSocketTransport != null)
            {
                _webSocketTransport.Dispose();
            }
        }
    }
}
