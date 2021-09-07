using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport
{
    public class TransportRouter : IObservable<TransportMessage>, IDisposable
    {
        public TransportRouter()
        {
            _onReceived = new Subject<TransportMessage>();
            _httpClient = new HttpClient();
            _http = new HttpPolling(_httpClient);
        }

        HttpPolling _http;
        IWebSocket _ws;
        readonly Subject<TransportMessage> _onReceived;
        readonly HttpClient _httpClient;
        string _pollingUri;

        public Uri ServerUri { get; set; }

        public TransportProtocol Protocol { get; private set; }

        public SocketIOOptions Options { get; set; }

        public string Sid { get; set; }

        public bool Connected { get; private set; }

        public Func<IWebSocket> WebSocketProvider { get; set; }

        public async Task HandshakeAsync()
        {
            string uri = GetHandshakeUri();
            //var resMsg = await _httpClient.GetAsync(uri).ConfigureAwait(false);
            //if (resMsg.IsSuccessStatusCode)
            //{
            //    await SwitchToWebSocketAsync().ConfigureAwait(false);
            //}
            //else
            //{
            //    await SwitchToPollingAsync().ConfigureAwait(false);
            //}
            string text = await _httpClient.GetStringAsync(uri).ConfigureAwait(false);

            int index = text.IndexOf('{');
            string json = text.Substring(index);
            var info = System.Text.Json.JsonSerializer.Deserialize<HandshakeInfo>(json);
            if (info.Upgrades.Contains("websocket"))
            {
                await SwitchToWebSocketAsync().ConfigureAwait(false);
            }
            else
            {
                await SwitchToPollingAsync().ConfigureAwait(false);
            }
        }

        private async Task SwitchToWebSocketAsync()
        {
            _ws = WebSocketProvider == null ? new WebSocket() : WebSocketProvider();
            Connected = false;
            Uri uri = GetWebSocketUri();
            await _ws.ConnectAsync(uri).ConfigureAwait(false);
            Protocol = TransportProtocol.WebSocket;
            Connected = true;
            _ws.Subscribe(_onReceived);
        }

        private async Task SwitchToPollingAsync()
        {
            if (_ws != null)
            {
                _ws.Dispose();
            }
            _http = new HttpPolling(_httpClient);
            Sid = null;
            Connected = false;
            Uri uri = GetHttpUri();
            await _http.GetAsync(uri.ToString(), CancellationToken.None).ConfigureAwait(false);
            Protocol = TransportProtocol.Polling;
            Connected = true;
            _http.Subscribe(_onReceived);
        }

        public async Task SendAsync(string text, CancellationToken cancellationToken)
        {
            if (Protocol == TransportProtocol.Polling)
            {
                if (_pollingUri == null)
                {
                    _pollingUri = GetHttpUri().ToString();
                }
                await _http.PostAsync(_pollingUri, text, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _ws.SendAsync(text, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SendAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            if (Protocol == TransportProtocol.Polling)
            {
                if (_pollingUri == null)
                {
                    _pollingUri = GetHttpUri().ToString();
                }
                await _http.PostAsync(_pollingUri, bytes, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _ws.SendAsync(bytes, cancellationToken).ConfigureAwait(false);
            }
        }

        private Uri GetWebSocketUri()
        {
            var builder = GetUriBuilder();
            if (ServerUri.Scheme == "https" || ServerUri.Scheme == "wss")
            {
                builder.Insert(0, "wss://");
            }
            else
            {
                builder.Insert(0, "ws://");
            }
            builder.Append("&transport=websocket");
            return new Uri(builder.ToString());
        }

        private StringBuilder GetUriBuilder()
        {
            var builder = new StringBuilder();
            builder.Append(ServerUri.Host);
            if (!ServerUri.IsDefaultPort)
            {
                builder.Append(":").Append(ServerUri.Port);
            }
            builder
                .Append(Options.Path)
                .Append("/?EIO=")
                .Append(Options.EIO);

            //if (QueryString != null)
            //{
            //    foreach (var item in QueryString)
            //    {
            //        builder
            //            .Append("&")
            //            .Append(item.Key)
            //            .Append("=")
            //            .Append(item.Value);
            //    }
            //}
            foreach (var item in Options.Query)
            {
                builder
                    .Append("&")
                    .Append(item.Key)
                    .Append("=")
                    .Append(item.Value);
            }
            return builder;
        }

        private string GetHandshakeUri()
        {
            var builder = GetUriBuilder();
            if (ServerUri.Scheme == "https" || ServerUri.Scheme == "wss")
            {
                builder.Insert(0, "https://");
            }
            else
            {
                builder.Insert(0, "http://");
            }
            builder
                .Append("&transport=polling");
            return builder.ToString();
        }

        private Uri GetHttpUri()
        {
            var builder = GetUriBuilder();
            if (ServerUri.Scheme == "https" || ServerUri.Scheme == "wss")
            {
                builder.Insert(0, "https://");
            }
            else
            {
                builder.Insert(0, "http://");
            }
            builder
                .Append("&transport=websocket")
                .Append("&t=")
                .Append(DateTimeOffset.Now.ToUnixTimeSeconds());
            if (!string.IsNullOrEmpty(Sid))
            {
                builder.Append("$sid=").Append(Sid);
            }
            return new Uri(builder.ToString());
        }

        public IDisposable Subscribe(IObserver<TransportMessage> observer)
        {
            //return _onReceived.Subscribe(x =>
            //{
            //    observer.OnNext(x);
            //});
            return _onReceived.Subscribe(observer);
        }

        public void Dispose()
        {
            if (_ws != null)
            {
                _ws.Dispose();
            }
            _httpClient.Dispose();
        }
    }
}
