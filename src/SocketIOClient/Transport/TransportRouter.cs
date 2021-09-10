using SocketIOClient.JsonSerializer;
using SocketIOClient.Messages;
using SocketIOClient.UriConverters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport
{
    public class TransportRouter : IDisposable
    {
        public TransportRouter(HttpClient httpClient, Func<IClientWebSocket> clientWebSocketProvider)
        {
            _httpClient = httpClient;
            _clientWebSocketProvider = clientWebSocketProvider;
            UriConverter = new UriConverter();
            Path = "/socket.io";
            _messageQueue = new Queue<IMessage>();
        }

        readonly HttpClient _httpClient;
        IClientWebSocket _clientWebSocket;
        readonly Queue<IMessage> _messageQueue;
        readonly Func<IClientWebSocket> _clientWebSocketProvider;

        HttpTransport _httpTransport;
        WebSocketTransport _webSocketTransport;
        CancellationTokenSource _pingTokenSource;
        CancellationToken _pingToken;
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

        public Action OnTransportClosed { get; set; }

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
                _clientWebSocket = _clientWebSocketProvider();
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
            _webSocketTransport.OnAborted = OnAborted;
            await _webSocketTransport.SendAsync("2probe", CancellationToken.None);
        }

        private async Task HttpConnectAsync()
        {
            _pingTokenSource = new CancellationTokenSource();
            _pingToken = _pingTokenSource.Token;
            _httpTransport.OnTextReceived = OnTextReceived;
            _httpTransport.OnBinaryReceived = OnBinaryReceived;

            StartPolling();
            var msg = new ConnectedMessage
            {
                Namespace = Namespace
            };
            await _httpTransport.PostAsync(_httpUri, msg.Write(), CancellationToken.None).ConfigureAwait(false);
        }

        private void StartPolling()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_pingToken.IsCancellationRequested)
                {
                    try
                    {
                        await _httpTransport.GetAsync(_httpUri, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch
                    {
                        OnTransportClosed();
                        throw;
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        private void OnWebSocketTextReceived(string text)
        {
            if (text == "3probe")
            {
                var msg = new ConnectedMessage { Namespace = Namespace, Sid = Sid };
                _ = _webSocketTransport.SendAsync("5", CancellationToken.None);
                _ = _webSocketTransport.SendAsync(msg.Write(), CancellationToken.None);
            }
            else
            {
                OnTextReceived(text);
            }
        }

        private void OnTextReceived(string text)
        {
            Debug.WriteLine($"[Receive] {text}");
            var msg = MessageFactory.CreateMessage(text);
            if (msg != null)
            {
                if (msg.BinaryCount > 0)
                {
                    //msg.IncomingBytes = new List<byte[]>(msg.BinaryCount);
                    //msg.IncomingBytes.AddRange(_incomingBytes);
                    //_incomingBytes.Clear();
                    msg.IncomingBytes = new List<byte[]>(msg.BinaryCount);
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
            Debug.WriteLine($"[Receive] binary message");
            //_incomingBytes.Add(bytes);
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

        private void OnAborted(Exception e)
        {
            OnTransportClosed();
        }

        public async Task SendAsync(IMessage msg, CancellationToken cancellationToken)
        {
            string text = msg.Write();
            await SendAsync(text, cancellationToken).ConfigureAwait(false);
            if (msg.OutgoingBytes != null)
            {
                await SendAsync(msg.OutgoingBytes, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task DisconnectAsync()
        {
            if (Protocol == TransportProtocol.Polling)
            {
                _pingTokenSource.Cancel();
            }
            else
            {
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
                _clientWebSocket.Dispose();
            }
        }

        private async Task SendAsync(string text, CancellationToken cancellationToken)
        {
            if (Protocol == TransportProtocol.Polling)
            {
                await _httpTransport.PostAsync(_httpUri, text, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _webSocketTransport.SendAsync(text, cancellationToken).ConfigureAwait(false);
            }
            Debug.WriteLine($"[Send] {text}");
        }

        private async Task SendAsync(IEnumerable<byte[]> bytes, CancellationToken cancellationToken)
        {
            if (Protocol == TransportProtocol.Polling)
            {
                await _httpTransport.PostAsync(_httpUri, bytes, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                foreach (var item in bytes)
                {
                    await _webSocketTransport.SendAsync(item, cancellationToken).ConfigureAwait(false);
                }
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
