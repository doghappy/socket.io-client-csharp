using SocketIOClient.JsonSerializer;
using SocketIOClient.Messages;
using SocketIOClient.UriConverters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Path = "/socket.io";
            _messageQueue = new Queue<IMessage>();
        }

        readonly HttpClient _httpClient;
        readonly IClientWebSocket _clientWebSocket;
        readonly Queue<IMessage> _messageQueue;

        HttpTransport _httpTransport;
        WebSocketTransport _webSocketTransport;
        string _httpUri;

        public Uri ServerUri { get; set; }

        public string Path { get; set; }

        public string Namespace { get; set; }

        public bool AutoUpgrade { get; set; }

        public IEnumerable<KeyValuePair<string, string>> QueryParams { get; set; }

        public TransportProtocol Protocol { get; private set; }

        public string Sid { get; private set; }

        public IUriConverter UriConverter { get; set; }

        public IJsonSerializer JsonSerializer { get; set; }

        public Action<IMessage> OnMessageReceived { get; set; }

        public async Task ConnectAsync()
        {
            if (_webSocketTransport != null)
            {
                _webSocketTransport.Dispose();
            }

            Uri uri = UriConverter.GetHandshakeUri(ServerUri, Path, QueryParams);
            string text = await _httpClient.GetStringAsync(uri).ConfigureAwait(false);
            int index = text.IndexOf('{');
            string json = text.Substring(index);
            var info = System.Text.Json.JsonSerializer.Deserialize<HandshakeInfo>(json);
            Sid = info.Sid;
            if (info.Upgrades.Contains("websocket") && AutoUpgrade)
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
            Uri uri = UriConverter.GetWebSocketUri(ServerUri, Path, QueryParams, Sid);
            await _webSocketTransport.ConnectAsync(uri).ConfigureAwait(false);
            _webSocketTransport.OnTextReceived = OnWebSocketTextReceived;
            _webSocketTransport.OnBinaryReceived = OnBinaryReceived;
            await _webSocketTransport.SendAsync("2probe", CancellationToken.None);
        }

        private async Task HttpConnectAsync()
        {
            _httpTransport.OnTextReceived = OnTextReceived;
            _httpTransport.OnBinaryReceived = OnBinaryReceived;
            var msg = new ConnectedMessage
            {
                Namespace = Namespace
            };
            await _httpTransport.PostAsync(_httpUri, msg.Write(), CancellationToken.None);
            StartPolling();
        }

        private void StartPolling()
        {
            var task = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    Debug.WriteLine("Polling...");
                    await _httpTransport.GetAsync(_httpUri, CancellationToken.None);
                }
            }, TaskCreationOptions.LongRunning);
            task.ContinueWith(t => Debug.WriteLine("Stop polling"));
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
            var msg = MessageFactory.CreateMessage(text);
            if (msg != null)
            {
                if (msg.BinaryCount > 0)
                {
                    _messageQueue.Enqueue(msg);
                }
                else
                {
                    OnMessageReceived(msg);
                }
            }
        }

        private void OnBinaryReceived(byte[] bytes)
        {
            if (_messageQueue.Count > 0)
            {
                var msg = _messageQueue.Peek();
                msg.IncomingBytes.Add(bytes);
                if (msg.IncomingBytes.Count == msg.BinaryCount)
                {
                    OnMessageReceived(msg);
                    _messageQueue.Dequeue();
                }
            }
        }

        public async Task SendAsync(IMessage msg, CancellationToken cancellationToken)
        {
            string text = msg.Write();
            await _httpTransport.PostAsync(_httpUri, text, cancellationToken).ConfigureAwait(false);
            if (msg.OutgoingBytes != null)
            {
                foreach (var item in msg.OutgoingBytes)
                {
                    await SendAsync(item, cancellationToken);
                }
            }
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
                string text = 'b' + Convert.ToBase64String(bytes);
                await _httpTransport.PostAsync(_httpUri, text, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _webSocketTransport.SendAsync(bytes, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _messageQueue.Clear();
            if (_webSocketTransport != null)
            {
                _webSocketTransport.Dispose();
            }
        }
    }
}
