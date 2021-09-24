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
        public TransportRouter(HttpClient httpClient, Func<IClientWebSocket> clientWebSocketProvider, SocketIOOptions options)
        {
            _httpClient = httpClient;
            _clientWebSocketProvider = clientWebSocketProvider;
            UriConverter = new UriConverter();
            _messageQueue = new Queue<IMessage>();
            _options = options;
        }

        readonly HttpClient _httpClient;
        IClientWebSocket _clientWebSocket;
        readonly Queue<IMessage> _messageQueue;
        readonly Func<IClientWebSocket> _clientWebSocketProvider;
        readonly SocketIOOptions _options;

        HttpTransport _httpTransport;
        WebSocketTransport _webSocketTransport;
        CancellationTokenSource _pollingTokenSource;
        string _httpUri;
        OpenedMessage _openedMessage;
        CancellationTokenSource _pingTokenSource;
        DateTime _pingTime;

        public Uri ServerUri { get; set; }

        public string Namespace { get; set; }

        public TransportProtocol Protocol { get; private set; }

        public int EIO { get; private set; }

        public IUriConverter UriConverter { get; set; }

        public Action<IMessage> OnMessageReceived { get; set; }

        public Action OnTransportClosed { get; set; }

        public async Task ConnectAsync()
        {
            Protocol = _options.Transport;
            EIO = _options.EIO;
            if (Protocol == TransportProtocol.WebSocket)
            {
                await ConnectByWebsocketAsync().ConfigureAwait(false);
            }
            else
            {
                await ConnectByPollingAsync().ConfigureAwait(false);
            }
        }

        private async Task ConnectByWebsocketAsync()
        {
            if (_webSocketTransport != null)
            {
                _webSocketTransport.Dispose();
            }
            Uri uri = UriConverter.GetServerUri(true, ServerUri, EIO, _options.Path, _options.Query);
            _clientWebSocket = _clientWebSocketProvider();
            if (_options.ExtraHeaders != null)
            {
                foreach (var item in _options.ExtraHeaders)
                {
                    _clientWebSocket.SetRequestHeader(item.Key, item.Value);
                }
            }
            _webSocketTransport = new WebSocketTransport(_clientWebSocket, EIO)
            {
                ConnectionTimeout = _options.ConnectionTimeout
            };
            _webSocketTransport.OnTextReceived = OnTextReceived;
            _webSocketTransport.OnBinaryReceived = OnBinaryReceived;
            _webSocketTransport.OnAborted = OnAborted;
            Debug.WriteLine($"[Websocket] Connecting");
            await _webSocketTransport.ConnectAsync(uri).ConfigureAwait(false);
            Debug.WriteLine($"[Websocket] Connected");
        }

        private async Task ConnectByPollingAsync()
        {
            Uri uri = UriConverter.GetServerUri(false, ServerUri, EIO, _options.Path, _options.Query);
            var req = new HttpRequestMessage(HttpMethod.Get, uri);
            if (_options.ExtraHeaders != null)
            {
                foreach (var item in _options.ExtraHeaders)
                {
                    req.Headers.Add(item.Key, item.Value);
                }
            }
            _httpTransport = new HttpTransport(_httpClient, EIO)
            {
                OnTextReceived = OnTextReceived,
                OnBinaryReceived = OnBinaryReceived
            };
            await _httpTransport.SendAsync(req, new CancellationTokenSource(_options.ConnectionTimeout).Token).ConfigureAwait(false);
            if (_pollingTokenSource != null)
            {
                _pollingTokenSource.Cancel();
            }
            _pollingTokenSource = new CancellationTokenSource();

            StartPolling(_pollingTokenSource.Token);
        }

        private void StartPolling(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
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

        private void StartPing(CancellationToken cancellationToken)
        {
            Debug.WriteLine($"[Ping] Interval: {_openedMessage.PingInterval}");
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_openedMessage.PingInterval);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    try
                    {
                        var ping = new PingMessage();
                        Debug.WriteLine($"[Ping] Sending");
                        await SendAsync(ping, CancellationToken.None).ConfigureAwait(false);
                        Debug.WriteLine($"[Ping] Has been sent");
                        _pingTime = DateTime.Now;
                        OnMessageReceived(ping);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"[Ping] Failed to send, {e.Message}");
                        OnTransportClosed();
                        throw;
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        private async Task OnOpened(OpenedMessage msg)
        {
            _openedMessage = msg;
            if (Protocol == TransportProtocol.Polling)
            {
                Uri uri = UriConverter.GetServerUri(false, ServerUri, EIO, _options.Path, _options.Query);
                _httpUri = uri + "&sid=" + _openedMessage.Sid;
            }
            var connectMsg = new ConnectedMessage
            {
                Namespace = Namespace,
                Eio = EIO,
                Query = _options.Query
            };
            await SendAsync(connectMsg.Write(), CancellationToken.None).ConfigureAwait(false);
        }

        private async void OnTextReceived(string text)
        {
            Debug.WriteLine($"[Receive] {text}");
            var msg = MessageFactory.CreateMessage(EIO, text);
            if (msg != null)
            {
                if (msg.BinaryCount > 0)
                {
                    msg.IncomingBytes = new List<byte[]>(msg.BinaryCount);
                    _messageQueue.Enqueue(msg);
                }
                else
                {
                    if (msg.Type == MessageType.Opened)
                    {
                        await OnOpened(msg as OpenedMessage).ConfigureAwait(false);
                    }
                    if (EIO == 3)
                    {
                        if (msg.Type == MessageType.Connected)
                        {
                            var connectMsg = msg as ConnectedMessage;
                            connectMsg.Sid = _openedMessage.Sid;
                            if ((string.IsNullOrEmpty(Namespace) && string.IsNullOrEmpty(connectMsg.Namespace)) || connectMsg.Namespace == Namespace)
                            {
                                if (_pingTokenSource != null)
                                {
                                    _pingTokenSource.Cancel();
                                }
                                _pingTokenSource = new CancellationTokenSource();
                                StartPing(_pingTokenSource.Token);
                            }
                            else
                            {
                                return;
                            }
                        }
                        else if (msg.Type == MessageType.Pong)
                        {
                            var pong = msg as PongMessage;
                            pong.Duration = DateTime.Now - _pingTime;
                        }
                    }

                    OnMessageReceived(msg);
                    if (msg.Type == MessageType.Ping)
                    {
                        _pingTime = DateTime.Now;
                        try
                        {
                            await SendAsync(new PongMessage
                            {
                                Eio = EIO,
                                Protocol = Protocol
                            }, CancellationToken.None).ConfigureAwait(false);
                            OnMessageReceived(new PongMessage
                            {
                                Eio = EIO,
                                Protocol = Protocol,
                                Duration = DateTime.Now - _pingTime
                            });
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                            OnTransportClosed();
                        }
                    }
                }
            }
        }

        private void OnBinaryReceived(byte[] bytes)
        {
            Debug.WriteLine($"[Receive] binary message");
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
            Debug.WriteLine($"[Websocket] Aborted, " + e.Message);
            OnTransportClosed();
        }

        public async Task SendAsync(IMessage msg, CancellationToken cancellationToken)
        {
            msg.Eio = EIO;
            msg.Protocol = Protocol;
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
                _pollingTokenSource.Cancel();
            }
            else
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
            }
            if (_pingTokenSource != null)
            {
                _pingTokenSource.Cancel();
            }
        }

        private async Task SendAsync(string text, CancellationToken cancellationToken)
        {
            if (Protocol == TransportProtocol.Polling)
            {
                if (EIO == 3)
                {
                    text = text.Length + ":" + text;
                }
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
