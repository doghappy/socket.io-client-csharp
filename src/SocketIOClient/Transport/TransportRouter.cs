using SocketIOClient.Messages;
using SocketIOClient.UriConverters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
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
        CancellationToken _pollingToken;
        string _httpUri;
        //int _pingInterval;
        OpenedMessage _openedMessage;
        CancellationTokenSource _pingTokenSource;
        CancellationToken _pingToken;
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

            //    if (_webSocketTransport != null)
            //    {
            //        _webSocketTransport.Dispose();
            //    }
            //Handshake:
            //    Uri uri = UriConverter.GetHandshakeUri(ServerUri, EIO, _options.Path, _options.Query);

            //    var req = new HttpRequestMessage(HttpMethod.Get, uri);
            //    SetHeaders(req);

            //    var resMsg = await _httpClient.SendAsync(req, new CancellationTokenSource(_options.ConnectionTimeout).Token).ConfigureAwait(false);
            //    if (!resMsg.IsSuccessStatusCode)
            //    {
            //        if (resMsg.StatusCode == HttpStatusCode.NotFound)
            //        {
            //            string errMsg = await resMsg.Content.ReadAsStringAsync().ConfigureAwait(false);
            //            if (errMsg.Contains("Transport unknown"))
            //            {

            //            }
            //        }
            //        throw new HttpRequestException($"Response status code does not indicate success: {resMsg.StatusCode}");
            //    }
            //    string text = await resMsg.Content.ReadAsStringAsync().ConfigureAwait(false);
            //    var openedMessage = MessageFactory.CreateOpenedMessage(text);

            //    if (openedMessage.EIO == 3 && EIO == 4)
            //    {
            //        EIO = 3;
            //        goto Handshake;
            //    }

            //    Sid = openedMessage.Sid;
            //    EIO = openedMessage.EIO;
            //    uri = UriConverter.GetHandshakeUri(ServerUri, EIO, _options.Path, _options.Query);
            //    _pingInterval = openedMessage.PingInterval;
            //    if (openedMessage.Upgrades.Contains("websocket") && _options.AutoUpgrade)
            //    {
            //        _clientWebSocket = _clientWebSocketProvider();
            //        _webSocketTransport = new WebSocketTransport(_clientWebSocket, EIO)
            //        {
            //            ConnectionTimeout = _options.ConnectionTimeout
            //        };
            //        await WebSocketConnectAsync().ConfigureAwait(false);
            //        Protocol = TransportProtocol.WebSocket;
            //    }
            //    else
            //    {
            //        _httpUri = uri + "&sid=" + Sid;
            //        _httpTransport = new HttpTransport(_httpClient, EIO);
            //        await HttpConnectAsync().ConfigureAwait(false);
            //        Protocol = TransportProtocol.Polling;
            //    }
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
            await _webSocketTransport.ConnectAsync(uri).ConfigureAwait(false);
            _webSocketTransport.OnTextReceived = OnTextReceived;
            _webSocketTransport.OnBinaryReceived = OnBinaryReceived;
            _webSocketTransport.OnAborted = OnAborted;
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
            //_openedMessage = MessageFactory.CreateOpenedMessage(text);
            //_httpUri = uri + "&sid=" + _openedMessage.Sid;
            await HttpConnectAsync().ConfigureAwait(false);
        }

        //private async Task WebSocketConnectAsync()
        //{
        //    Uri uri = UriConverter.GetWebSocketUri(ServerUri, EIO, _options.Path, _options.Query, Sid);
        //    await _webSocketTransport.ConnectAsync(uri).ConfigureAwait(false);
        //    _webSocketTransport.OnTextReceived = OnWebSocketTextReceived;
        //    _webSocketTransport.OnBinaryReceived = OnBinaryReceived;
        //    _webSocketTransport.OnAborted = OnAborted;
        //    await _webSocketTransport.SendAsync("2probe", CancellationToken.None);
        //}

        private async Task HttpConnectAsync()
        {
            _pollingTokenSource = new CancellationTokenSource();
            _pollingToken = _pollingTokenSource.Token;

            StartPolling();

            //if (!(EIO == 3 && string.IsNullOrEmpty(Namespace)))
            //{
            //    var msg = new ConnectedMessage
            //    {
            //        Namespace = Namespace,
            //        Eio = EIO,
            //        Protocol = TransportProtocol.Polling,
            //        Query = _options.Query
            //    };
            //    await SendAsync(msg.Write(), CancellationToken.None).ConfigureAwait(false);
            //}
        }

        private void StartPolling()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_pollingToken.IsCancellationRequested)
                {
                    try
                    {
                        await _httpTransport.GetAsync(_httpUri, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException e)
                    {
                        Debug.WriteLine(e);
                        break;
                    }
                    catch
                    {
                        OnTransportClosed();
                        throw;
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        private async Task PingAsync()
        {
            Debug.WriteLine($"PingInterval: {_openedMessage.PingInterval}");
            while (!_pingToken.IsCancellationRequested)
            {
                await Task.Delay(_openedMessage.PingInterval);
                try
                {
                    var ping = new PingMessage();
                    Debug.WriteLine($"Send Ping");
                    await SendAsync(ping, CancellationToken.None).ConfigureAwait(false);
                    _pingTime = DateTime.Now;
                    OnMessageReceived(ping);
                }
                catch
                {
                    OnTransportClosed();
                    throw;
                }
            }
        }

        //private async void OnWebSocketTextReceived(string text)
        //{
        //    if (text == "3probe")
        //    {
        //        await _webSocketTransport.SendAsync("5", CancellationToken.None);

        //        if (EIO == 3 && string.IsNullOrEmpty(Namespace))
        //        {
        //            return;
        //        }
        //        var msg = new ConnectedMessage
        //        {
        //            Namespace = Namespace,
        //            Eio = EIO,
        //            Sid = Sid,
        //            Protocol = TransportProtocol.WebSocket,
        //            Query = _options.Query
        //        };
        //        await _webSocketTransport.SendAsync(msg.Write(), CancellationToken.None);
        //    }
        //    else
        //    {
        //        OnTextReceived(text);
        //    }
        //}
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
                                    _pingTokenSource = new CancellationTokenSource();
                                    _pingToken = _pingTokenSource.Token;
                                }
                                _ = Task.Factory.StartNew(PingAsync, TaskCreationOptions.LongRunning);
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
            if (_pingTokenSource != null)
            {
                _pingTokenSource.Cancel();
            }
        }
    }
}
