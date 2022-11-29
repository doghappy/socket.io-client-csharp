using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Extensions;
using SocketIOClient.JsonSerializer;
using SocketIOClient.Messages;
using SocketIOClient.Transport;
using SocketIOClient.Transport.Http;
using SocketIOClient.Transport.WebSockets;
using SocketIOClient.UriConverters;

namespace SocketIOClient
{
    /// <summary>
    /// socket.io client class
    /// </summary>
    public class SocketIO : IDisposable
    {
        /// <summary>
        /// Create SocketIO object with default options
        /// </summary>
        /// <param name="uri"></param>
        public SocketIO(string uri) : this(new Uri(uri))
        {
        }

        /// <summary>
        /// Create SocketIO object with options
        /// </summary>
        /// <param name="uri"></param>
        public SocketIO(Uri uri) : this(uri, new SocketIOOptions())
        {
        }

        /// <summary>
        /// Create SocketIO object with options
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="options"></param>
        public SocketIO(string uri, SocketIOOptions options) : this(new Uri(uri), options)
        {
        }

        /// <summary>
        /// Create SocketIO object with options
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="options"></param>
        public SocketIO(Uri uri, SocketIOOptions options)
        {
            ServerUri = uri ?? throw new ArgumentNullException("uri");
            Options = options ?? throw new ArgumentNullException("options");
            Initialize();
        }

        Uri _serverUri;

        private Uri ServerUri
        {
            get => _serverUri;
            set
            {
                if (_serverUri != value)
                {
                    _serverUri = value;
                    if (value != null && value.AbsolutePath != "/")
                    {
                        _namespace = value.AbsolutePath;
                    }
                }
            }
        }

        /// <summary>
        /// An unique identifier for the socket session. Set after the connect event is triggered, and updated after the reconnect event.
        /// </summary>
        public string Id { get; private set; }

        string _namespace;

        /// <summary>
        /// Whether or not the socket is connected to the server.
        /// </summary>
        public bool Connected { get; private set; }

        int _attempts;

        [Obsolete]
        /// <summary>
        /// Whether or not the socket is disconnected from the server.
        /// </summary>
        public bool Disconnected => !Connected;

        public SocketIOOptions Options { get; }

        public IJsonSerializer JsonSerializer { get; set; }

        public HttpClient HttpClient { get; set; }

        public Func<IClientWebSocket> ClientWebSocketProvider { get; set; }
        public Func<IHttpClient> HttpClientAdapterProvider { get; set; }

        List<IDisposable> _resources = new List<IDisposable>();

        BaseTransport _transport;

        List<Type> _expectedExceptions;

        int _packetId;
        Exception _backgroundException;
        Dictionary<int, Action<SocketIOResponse>> _ackHandlers;
        List<OnAnyHandler> _onAnyHandlers;
        Dictionary<string, Action<SocketIOResponse>> _eventHandlers;
        double _reconnectionDelay;

        #region Socket.IO event

        public event EventHandler OnConnected;

        //public event EventHandler<string> OnConnectError;
        //public event EventHandler<string> OnConnectTimeout;
        public event EventHandler<string> OnError;
        public event EventHandler<string> OnDisconnected;

        /// <summary>
        /// Fired upon a successful reconnection.
        /// </summary>
        public event EventHandler<int> OnReconnected;

        /// <summary>
        /// Fired upon an attempt to reconnect.
        /// </summary>
        public event EventHandler<int> OnReconnectAttempt;

        /// <summary>
        /// Fired upon a reconnection attempt error.
        /// </summary>
        public event EventHandler<Exception> OnReconnectError;

        /// <summary>
        /// Fired when couldn’t reconnect within reconnectionAttempts
        /// </summary>
        public event EventHandler OnReconnectFailed;

        public event EventHandler OnPing;
        public event EventHandler<TimeSpan> OnPong;

        #endregion

        #region Observable Event

        //Subject<Unit> _onConnected;
        //public IObservable<Unit> ConnectedObservable { get; private set; }

        #endregion

        private void Initialize()
        {
            _packetId = -1;
            _ackHandlers = new Dictionary<int, Action<SocketIOResponse>>();
            _eventHandlers = new Dictionary<string, Action<SocketIOResponse>>();
            _onAnyHandlers = new List<OnAnyHandler>();

            JsonSerializer = new SystemTextJsonSerializer();

            HttpClient = new HttpClient();
            ClientWebSocketProvider = () => new DefaultClientWebSocket();
            HttpClientAdapterProvider = () => new DefaultHttpClient();
            _expectedExceptions = new List<Type>
            {
                typeof(TimeoutException),
                typeof(WebSocketException),
                typeof(HttpRequestException),
                typeof(OperationCanceledException),
                typeof(TaskCanceledException)
            };
        }

        private async Task InitTransportAsync()
        {
            Options.Transport = await GetProtocolAsync();
            var transportOptions = new TransportOptions
            {
                EIO = Options.EIO,
                Query = Options.Query,
                Auth = GetAuth(Options.Auth),
                ConnectionTimeout = Options.ConnectionTimeout
            };
            if (Options.Transport == TransportProtocol.Polling)
            {
                var adapter = HttpClientAdapterProvider();
                if (adapter is null)
                {
                    throw new ArgumentNullException(nameof(HttpClientAdapterProvider), $"{HttpClientAdapterProvider} returns a null");
                }
                _resources.Add(adapter);
                var handler = HttpPollingHandler.CreateHandler(transportOptions.EIO, adapter);
                _transport = new HttpTransport(transportOptions, handler);
            }
            else
            {
                var ws = ClientWebSocketProvider();
                if (ws is null)
                {
                    throw new ArgumentNullException(nameof(ClientWebSocketProvider), $"{ClientWebSocketProvider} returns a null");
                }
                _resources.Add(ws);
                _transport = new WebSocketTransport(transportOptions, ws);
            }
            _resources.Add(_transport);
            _transport.Namespace = _namespace;
            SetHeaders();
            _transport.SetProxy(Options.Proxy);
            _transport.OnReceived = OnMessageReceived;
            _transport.OnError = OnErrorReceived;
        }

        private string GetAuth(object auth)
        {
            if (auth == null)
                return string.Empty;
            var result = JsonSerializer.Serialize(new[] { auth });
            return result.Json.TrimStart('[').TrimEnd(']');
        }

        private void SetHeaders()
        {
            if (Options.ExtraHeaders != null)
            {
                foreach (var item in Options.ExtraHeaders)
                {
                    try
                    {
                        _transport.AddHeader(item.Key, item.Value);
                    }
                    catch (Exception e)
                    {
                        OnErrorReceived(e);
                    }
                }
            }
        }

        private void DisposeResources()
        {
            foreach (var item in _resources)
            {
                item.TryDispose();
            }
            _resources.Clear();
        }

        private void ConnectInBackground(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    DisposeResources();
                    await InitTransportAsync().ConfigureAwait(false);
                    var serverUri = UriConverter.GetServerUri(Options.Transport == TransportProtocol.WebSocket, ServerUri, Options.EIO, Options.Path, Options.Query);
                    if (_attempts > 0)
                        OnReconnectAttempt.TryInvoke(this, _attempts);
                    try
                    {
                        using (var cts = new CancellationTokenSource(Options.ConnectionTimeout))
                        {
                            await _transport.ConnectAsync(serverUri, cts.Token).ConfigureAwait(false);
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        var needBreak = await AttemptAsync(e);
                        if (needBreak) break;

                        var canHandle = CanHandleException(e);
                        if (!canHandle) throw e;
                    }
                }
            }, cancellationToken);
        }

        private async Task<bool> AttemptAsync(Exception e)
        {
            if (_attempts > 0)
            {
                OnReconnectError.TryInvoke(this, e);
            }
            _attempts++;
            if (_attempts <= Options.ReconnectionAttempts)
            {
                if (_reconnectionDelay < Options.ReconnectionDelayMax)
                {
                    _reconnectionDelay += 2 * Options.RandomizationFactor;
                }
                if (_reconnectionDelay > Options.ReconnectionDelayMax)
                {
                    _reconnectionDelay = Options.ReconnectionDelayMax;
                }
                await Task.Delay((int)_reconnectionDelay);
            }
            else
            {
                OnReconnectFailed.TryInvoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        private bool CanHandleException(Exception e)
        {
            if (_expectedExceptions.Contains(e.GetType()))
            {
                if (!Options.Reconnection)
                {
                    _backgroundException = e;
                    return false;
                }
            }
            else
            {
                _backgroundException = e;
                return false;
            }
            return true;
        }

        private async Task<TransportProtocol> GetProtocolAsync()
        {
            if (Options.Transport == TransportProtocol.Polling && Options.AutoUpgrade)
            {
                Uri uri = UriConverter.GetServerUri(false, ServerUri, Options.EIO, Options.Path, Options.Query);
                try
                {
                    string text = await HttpClient.GetStringAsync(uri);
                    if (text.Contains("websocket"))
                    {
                        return TransportProtocol.WebSocket;
                    }
                }
                catch (Exception e)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(e);
#endif
                }
            }
            return Options.Transport;
        }

        private readonly SemaphoreSlim _connectingLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _connCts;

        private void ConnectInBackground()
        {
            _connCts.TryCancel();
            _connCts.TryDispose();
            _connCts = new CancellationTokenSource();
            ConnectInBackground(_connCts.Token);
        }

        public async Task ConnectAsync()
        {
            await _connectingLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (Connected) return;
                
                ConnectInBackground();

                var ms = 0;
                while (true)
                {
                    if (_connCts.IsCancellationRequested)
                    {
                        break;
                    }

                    if (_backgroundException != null)
                    {
                        throw new ConnectionException($"Cannot connect to server '{ServerUri}'", _backgroundException);
                    }

                    ms += 100;
                    if (ms > Options.ConnectionTimeout.TotalMilliseconds)
                    {
                        throw new ConnectionException($"Cannot connect to server '{ServerUri}'", new TimeoutException());
                    }
                    await Task.Delay(100);
                }
            }
            finally
            {
                _connectingLock.Release();
            }
        }

        private void PingHandler()
        {
            OnPing.TryInvoke(this, EventArgs.Empty);
        }

        private void PongHandler(PongMessage msg)
        {
            OnPong.TryInvoke(this, msg.Duration);
        }

        private void ConnectedHandler(ConnectedMessage msg)
        {
            Id = msg.Sid;
            Connected = true;
            _connCts.Cancel();
            OnConnected.TryInvoke(this, EventArgs.Empty);
            if (_attempts > 0)
            {
                OnReconnected.TryInvoke(this, _attempts);
            }
            _attempts = 0;
        }

        private void DisconnectedHandler()
        {
            _ = InvokeDisconnect(DisconnectReason.IOServerDisconnect);
        }

        private void EventMessageHandler(EventMessage m)
        {
            var res = new SocketIOResponse(m.JsonElements, this)
            {
                PacketId = m.Id
            };
            foreach (var item in _onAnyHandlers)
            {
                item.TryInvoke(m.Event, res);
            }
            if (_eventHandlers.ContainsKey(m.Event))
            {
                _eventHandlers[m.Event].TryInvoke(res);
            }
        }

        private void AckMessageHandler(ClientAckMessage m)
        {
            if (_ackHandlers.ContainsKey(m.Id))
            {
                var res = new SocketIOResponse(m.JsonElements, this);
                _ackHandlers[m.Id].TryInvoke(res);
                _ackHandlers.Remove(m.Id);
            }
        }

        private void ErrorMessageHandler(ErrorMessage msg)
        {
            OnError.TryInvoke(this, msg.Message);
        }

        private void BinaryMessageHandler(BinaryMessage msg)
        {
            var response = new SocketIOResponse(msg.JsonElements, this)
            {
                PacketId = msg.Id,
            };
            response.InComingBytes.AddRange(msg.IncomingBytes);
            foreach (var item in _onAnyHandlers)
            {
                item.TryInvoke(msg.Event, response);
            }
            if (_eventHandlers.ContainsKey(msg.Event))
            {
                _eventHandlers[msg.Event].TryInvoke(response);
            }
        }

        private void BinaryAckMessageHandler(ClientBinaryAckMessage msg)
        {
            if (_ackHandlers.ContainsKey(msg.Id))
            {
                var response = new SocketIOResponse(msg.JsonElements, this)
                {
                    PacketId = msg.Id,
                };
                response.InComingBytes.AddRange(msg.IncomingBytes);
                _ackHandlers[msg.Id].TryInvoke(response);
            }
        }

        private void OnErrorReceived(Exception ex)
        {
            //Logger.LogError(ex, ex.Message);
            _ = InvokeDisconnect(DisconnectReason.TransportClose);
        }

        private void OnMessageReceived(IMessage msg)
        {
            try
            {
                switch (msg.Type)
                {
                    case MessageType.Ping:
                        PingHandler();
                        break;
                    case MessageType.Pong:
                        PongHandler(msg as PongMessage);
                        break;
                    case MessageType.Connected:
                        ConnectedHandler(msg as ConnectedMessage);
                        break;
                    case MessageType.Disconnected:
                        DisconnectedHandler();
                        break;
                    case MessageType.EventMessage:
                        EventMessageHandler(msg as EventMessage);
                        break;
                    case MessageType.AckMessage:
                        AckMessageHandler(msg as ClientAckMessage);
                        break;
                    case MessageType.ErrorMessage:
                        ErrorMessageHandler(msg as ErrorMessage);
                        break;
                    case MessageType.BinaryMessage:
                        BinaryMessageHandler(msg as BinaryMessage);
                        break;
                    case MessageType.BinaryAckMessage:
                        BinaryAckMessageHandler(msg as ClientBinaryAckMessage);
                        break;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e);
#endif
            }
        }

        public async Task DisconnectAsync()
        {
            if (Connected)
            {
                var msg = new DisconnectedMessage
                {
                    Namespace = _namespace
                };
                try
                {
                    await _transport.SendAsync(msg, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(e);
#endif
                }
                await InvokeDisconnect(DisconnectReason.IOClientDisconnect);
            }
        }

        /// <summary>
        /// Register a new handler for the given event.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="callback"></param>
        public void On(string eventName, Action<SocketIOResponse> callback)
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers.Remove(eventName);
            }
            _eventHandlers.Add(eventName, callback);
        }


        /// <summary>
        /// Unregister a new handler for the given event.
        /// </summary>
        /// <param name="eventName"></param>
        public void Off(string eventName)
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers.Remove(eventName);
            }
        }

        public void OnAny(OnAnyHandler handler)
        {
            if (handler != null)
            {
                _onAnyHandlers.Add(handler);
            }
        }

        public void PrependAny(OnAnyHandler handler)
        {
            if (handler != null)
            {
                _onAnyHandlers.Insert(0, handler);
            }
        }

        public void OffAny(OnAnyHandler handler)
        {
            if (handler != null)
            {
                _onAnyHandlers.Remove(handler);
            }
        }

        public OnAnyHandler[] ListenersAny() => _onAnyHandlers.ToArray();

        internal async Task ClientAckAsync(int packetId, CancellationToken cancellationToken, params object[] data)
        {
            IMessage msg;
            if (data != null && data.Length > 0)
            {
                var result = JsonSerializer.Serialize(data);
                if (result.Bytes.Count > 0)
                {
                    msg = new ServerBinaryAckMessage
                    {
                        Id = packetId,
                        Namespace = _namespace,
                        Json = result.Json
                    };
                    msg.OutgoingBytes = new List<byte[]>(result.Bytes);
                }
                else
                {
                    msg = new ServerAckMessage
                    {
                        Namespace = _namespace,
                        Id = packetId,
                        Json = result.Json
                    };
                }
            }
            else
            {
                msg = new ServerAckMessage
                {
                    Namespace = _namespace,
                    Id = packetId
                };
            }
            await _transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Emits an event to the socket
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="data">Any other parameters can be included. All serializable datastructures are supported, including byte[]</param>
        /// <returns></returns>
        public async Task EmitAsync(string eventName, params object[] data)
        {
            await EmitAsync(eventName, CancellationToken.None, data).ConfigureAwait(false);
        }

        public async Task EmitAsync(string eventName, CancellationToken cancellationToken, params object[] data)
        {
            if (data != null && data.Length > 0)
            {
                var result = JsonSerializer.Serialize(data);
                if (result.Bytes.Count > 0)
                {
                    var msg = new BinaryMessage
                    {
                        Namespace = _namespace,
                        OutgoingBytes = new List<byte[]>(result.Bytes),
                        Event = eventName,
                        Json = result.Json
                    };
                    await _transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var msg = new EventMessage
                    {
                        Namespace = _namespace,
                        Event = eventName,
                        Json = result.Json
                    };
                    await _transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                var msg = new EventMessage
                {
                    Namespace = _namespace,
                    Event = eventName
                };
                await _transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Emits an event to the socket
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="ack">will be called with the server answer.</param>
        /// <param name="data">Any other parameters can be included. All serializable datastructures are supported, including byte[]</param>
        /// <returns></returns>
        public async Task EmitAsync(string eventName, Action<SocketIOResponse> ack, params object[] data)
        {
            await EmitAsync(eventName, CancellationToken.None, ack, data).ConfigureAwait(false);
        }

        public async Task EmitAsync(string eventName, CancellationToken cancellationToken, Action<SocketIOResponse> ack, params object[] data)
        {
            _ackHandlers.Add(++_packetId, ack);
            if (data != null && data.Length > 0)
            {
                var result = JsonSerializer.Serialize(data);
                if (result.Bytes.Count > 0)
                {
                    var msg = new ClientBinaryAckMessage
                    {
                        Event = eventName,
                        Namespace = _namespace,
                        Json = result.Json,
                        Id = _packetId,
                        OutgoingBytes = new List<byte[]>(result.Bytes)
                    };
                    await _transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var msg = new ClientAckMessage
                    {
                        Event = eventName,
                        Namespace = _namespace,
                        Id = _packetId,
                        Json = result.Json
                    };
                    await _transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                var msg = new ClientAckMessage
                {
                    Event = eventName,
                    Namespace = _namespace,
                    Id = _packetId
                };
                await _transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task InvokeDisconnect(string reason)
        {
            if (Connected)
            {
                Connected = false;
                Id = null;
                OnDisconnected.TryInvoke(this, reason);
                try
                {
                    await _transport.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(e);
#endif
                }
                if (reason != DisconnectReason.IOServerDisconnect && reason != DisconnectReason.IOClientDisconnect)
                {
                    //In the this cases (explicit disconnection), the client will not try to reconnect and you need to manually call socket.connect().
                    if (Options.Reconnection)
                    {
                        ConnectInBackground();
                    }
                }
            }
        }

        public void AddExpectedException(Type type)
        {
            if (!_expectedExceptions.Contains(type))
            {
                _expectedExceptions.Add(type);
            }
        }

        public void Dispose()
        {
            HttpClient.Dispose();
            _transport.TryDispose();
            _ackHandlers.Clear();
            _onAnyHandlers.Clear();
            _eventHandlers.Clear();
        }
    }
}