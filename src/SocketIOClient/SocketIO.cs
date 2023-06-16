using System;
using System.Collections.Generic;
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
using System.Text.Json;

#if DEBUG
using System.Diagnostics;
#endif

namespace SocketIOClient
{
    /// <summary>
    /// socket.io client class with generic type, currently 2 supported
    /// <para><c>JsonElement</c> from <c>System.Text.Json </c></para>
    /// OR
    /// <para><c>JToken</c> from <c>Newtonsoft.Json.Linq</c></para>
    /// </summary>

    public class SocketIO<T> : IDisposable
    {
        /// <summary>
        /// Create SocketIO object with default options
        /// </summary>
        /// <param name="uri"></param>
        public SocketIO(string uri) : this(new Uri(uri))
        {
        }

        /// <summary>
        /// Create SocketIO object with default options and JSON serializer
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="serializer"></param>
        public SocketIO(string uri, IJsonSerializer serializer) : this(new Uri(uri), serializer)
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
        /// Create SocketIO object with options and JSON Serializer
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="uri"></param>
        public SocketIO(Uri uri, IJsonSerializer serializer) : this(uri, new SocketIOOptions(), serializer)
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
        /// Create SocketIO object with options and JSON serializer
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        public SocketIO(string uri, SocketIOOptions options, IJsonSerializer serializer) : this(new Uri(uri), options, serializer)
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
            Initialize(new SystemTextJsonSerializer());
        }

        /// <summary>
        /// Create SocketIO object with options and JSON Serializer
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        public SocketIO(Uri uri, SocketIOOptions options, IJsonSerializer serializer)
        {
            if (!IsValidType(typeof(T)))
            {
                throw new ArgumentException($"Invalid type '{typeof(T)}' for SocketIO<T> valid options are JToken from Newtonsoft.Json.Linq and JsonElement from System.Text.Json");
            }
            ServerUri = uri ?? throw new ArgumentNullException("uri");
            Options = options ?? throw new ArgumentNullException("options");
            if (serializer == null) throw new ArgumentNullException("options");
            Initialize(serializer);
        }
        private bool IsValidType(Type type)
        {
            switch (type.Name.ToLower())
            {
                case "jtoken":
                case "jsonelement":
                    return true;
                default:
                    return false;
            }

        }
        Uri _serverUri;

        public Uri ServerUri
        {
            get => _serverUri;
            set
            {
                if (_serverUri != value)
                {
                    _serverUri = value;
                    if (value != null && value.AbsolutePath != "/")
                    {
                        Namespace = value.AbsolutePath;
                    }
                }
            }
        }

        /// <summary>
        /// An unique identifier for the socket session. Set after the connect event is triggered, and updated after the reconnect event.
        /// </summary>
        public string Id { get; private set; }

        public string Namespace { get; private set; }

        /// <summary>
        /// Whether or not the socket is connected to the server.
        /// </summary>
        public bool Connected { get; private set; }

        int _attempts;

        public SocketIOOptions Options { get; }

        public IJsonSerializer JsonSerializer { get; private set; }
        public ITransport Transport { get; set; }
        public IHttpClient HttpClient { get; set; }

        public Func<IClientWebSocket> ClientWebSocketProvider { get; set; }

        List<IDisposable> _resources = new List<IDisposable>();

        List<Type> _expectedExceptions;

        int _packetId;
        Exception _backgroundException;
        Dictionary<int, Action<SocketIOResponse<T>>> _ackHandlers;
        List<OnAnyHandler<T>> _onAnyHandlers;
        Dictionary<string, Action<SocketIOResponse<T>>> _eventHandlers;
        double _reconnectionDelay;
        bool _exitFromBackground;

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

        private void Initialize(IJsonSerializer serializer)
        {
            _packetId = -1;
            _ackHandlers = new Dictionary<int, Action<SocketIOResponse<T>>>();
            _eventHandlers = new Dictionary<string, Action<SocketIOResponse<T>>>();
            _onAnyHandlers = new List<OnAnyHandler<T>>();

            //JsonSerializer = new SystemTextJsonSerializer();
            JsonSerializer = serializer;

            HttpClient = new DefaultHttpClient();
            ClientWebSocketProvider = () => new DefaultClientWebSocket();
            _expectedExceptions = new List<Type>
            {
                typeof(TimeoutException),
                typeof(WebSocketException),
                typeof(HttpRequestException),
                typeof(OperationCanceledException),
                typeof(TaskCanceledException),
                typeof(TransportException),
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
                var handler = HttpPollingHandler.CreateHandler(transportOptions.EIO, HttpClient);
                Transport = new HttpTransport<T>(transportOptions, handler, JsonSerializer);
            }
            else
            {
                var ws = ClientWebSocketProvider();
                if (ws is null)
                {
                    throw new ArgumentNullException(nameof(ClientWebSocketProvider),
                        $"{ClientWebSocketProvider} returns a null");
                }

                _resources.Add(ws);
                Transport = new WebSocketTransport<T>(transportOptions, ws, JsonSerializer);
                SetWebSocketHeaders();
            }

            _resources.Add(Transport);
            Transport.Namespace = Namespace;
            if (Options.Proxy != null)
            {
                Transport.SetProxy(Options.Proxy);
            }

            Transport.OnReceived = OnMessageReceived;
            Transport.OnError = OnErrorReceived;
        }

        private string GetAuth(object auth)
        {
            if (auth == null)
                return string.Empty;
            var result = JsonSerializer.Serialize(new[] { auth });
            return result.Json.TrimStart('[').TrimEnd(']');
        }

        private void SetWebSocketHeaders()
        {
            if (Options.ExtraHeaders is null)
            {
                return;
            }

            foreach (var item in Options.ExtraHeaders)
            {
                Transport.AddHeader(item.Key, item.Value);
            }
        }

        private void SetHttpHeaders()
        {
            if (Options.ExtraHeaders is null)
            {
                return;
            }

            foreach (var header in Options.ExtraHeaders)
            {
                HttpClient.AddHeader(header.Key, header.Value);
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
            _reconnectionDelay = Options.ReconnectionDelay;
            Task.Factory.StartNew(async () =>
            {
                _exitFromBackground = false;
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    DisposeResources();
                    await InitTransportAsync().ConfigureAwait(false);
                    var serverUri = UriConverter.GetServerUri(Options.Transport == TransportProtocol.WebSocket,
                        ServerUri, Options.EIO, Options.Path, Options.Query);
                    if (_attempts > 0)
                        OnReconnectAttempt.TryInvoke(this, _attempts);
                    try
                    {
                        using (var cts = new CancellationTokenSource(Options.ConnectionTimeout))
                        {
                            await Transport.ConnectAsync(serverUri, cts.Token).ConfigureAwait(false);
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OnReconnectError.TryInvoke(this, e);
                        var needBreak = await AttemptAsync(e);
                        if (needBreak)
                        {
                            _exitFromBackground = true;
                            break;
                        }

                        var canHandle = CanHandleException(e);
                        if (!canHandle)
                        {
                            _exitFromBackground = true;
                            throw;
                        }
                    }
                }
            }, cancellationToken);
        }

        private async Task<bool> AttemptAsync(Exception e)
        {
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
                if (Options.Reconnection)
                {
                    if (_attempts > Options.ReconnectionAttempts)
                    {
                        return false;
                    }
                }
                else
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
            SetHttpHeaders();
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
                    Debug.WriteLine(e);
#endif
                }
            }

            return Options.Transport;
        }

        private readonly SemaphoreSlim _connectingLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSourceWrapper _connCts;

        private void ConnectInBackground()
        {
            _connCts = _connCts.Renew();
            ConnectInBackground(_connCts.Token);
        }

        public async Task ConnectAsync()
        {
            await _connectingLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (Connected) return;

                ConnectInBackground();

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

                    if (Options.Reconnection && _attempts > Options.ReconnectionAttempts)
                    {
                        throw new ConnectionException(
                            $"Cannot connect to server '{ServerUri}' after {_attempts} attempts.");
                    }

                    if (_exitFromBackground)
                    {
                        throw new ConnectionException($"Cannot connect to server '{ServerUri}'.");
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

        private void PongHandler(PongMessage<T> msg)
        {
            OnPong.TryInvoke(this, msg.Duration);
        }

        private void ConnectedHandler(ConnectedMessage<T> msg)
        {
            Id = msg.Sid;
            Connected = true;
            _connCts.Dispose();
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

        private void EventMessageHandler(EventMessage<T> m)
        {
            var res = new SocketIOResponse<T>(m.JsonElements, this)
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

        private void AckMessageHandler(ClientAckMessage<T> m)
        {
            if (_ackHandlers.ContainsKey(m.Id))
            {
                var res = new SocketIOResponse<T>(m.JsonElements, this);
                _ackHandlers[m.Id].TryInvoke(res);
                _ackHandlers.Remove(m.Id);
            }
        }

        private void ErrorMessageHandler(ErrorMessage<T> msg)
        {
            _connCts.Dispose();
            OnError.TryInvoke(this, msg.Message);
        }

        private void BinaryMessageHandler(BinaryMessage<T> msg)
        {
            var response = new SocketIOResponse<T>(msg.JsonElements, this)
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

        private void BinaryAckMessageHandler(ClientBinaryAckMessage<T> msg)
        {
            if (_ackHandlers.ContainsKey(msg.Id))
            {
                var response = new SocketIOResponse<T>(msg.JsonElements, this)
                {
                    PacketId = msg.Id,
                };
                response.InComingBytes.AddRange(msg.IncomingBytes);
                _ackHandlers[msg.Id].TryInvoke(response);
            }
        }

        private void OnErrorReceived(Exception ex)
        {
#if DEBUG
            Debug.WriteLine(ex);
#endif
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
                        PongHandler(msg as PongMessage<T>);
                        break;
                    case MessageType.Connected:
                        ConnectedHandler(msg as ConnectedMessage<T>);
                        break;
                    case MessageType.Disconnected:
                        DisconnectedHandler();
                        break;
                    case MessageType.EventMessage:
                        EventMessageHandler(msg as EventMessage<T>);
                        break;
                    case MessageType.AckMessage:
                        AckMessageHandler(msg as ClientAckMessage<T>);
                        break;
                    case MessageType.ErrorMessage:
                        ErrorMessageHandler(msg as ErrorMessage<T>);
                        break;
                    case MessageType.BinaryMessage:
                        BinaryMessageHandler(msg as BinaryMessage<T>);
                        break;
                    case MessageType.BinaryAckMessage:
                        BinaryAckMessageHandler(msg as ClientBinaryAckMessage<T>);
                        break;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Debug.WriteLine(e);
#endif
            }
        }

        public async Task DisconnectAsync()
        {
            _connCts.TryDispose();
            var msg = new DisconnectedMessage<T>
            {
                Namespace = Namespace
            };
            try
            {
                await Transport.SendAsync(msg, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
#if DEBUG
                Debug.WriteLine(e);
#endif
            }

            await InvokeDisconnect(DisconnectReason.IOClientDisconnect);
        }

        /// <summary>
        /// Register a new handler for the given event.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="callback"></param>
        public void On(string eventName, Action<SocketIOResponse<T>> callback)
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

        public void OnAny(OnAnyHandler<T> handler)
        {
            if (handler != null)
            {
                _onAnyHandlers.Add(handler);
            }
        }

        public void PrependAny(OnAnyHandler<T> handler)
        {
            if (handler != null)
            {
                _onAnyHandlers.Insert(0, handler);
            }
        }

        public void OffAny(OnAnyHandler<T> handler)
        {
            if (handler != null)
            {
                _onAnyHandlers.Remove(handler);
            }
        }

        public OnAnyHandler<T>[] ListenersAny() => _onAnyHandlers.ToArray();

        internal async Task ClientAckAsync(int packetId, CancellationToken cancellationToken, params object[] data)
        {
            IMessage msg;
            if (data != null && data.Length > 0)
            {
                var result = JsonSerializer.Serialize(data);
                if (result.Bytes.Count > 0)
                {
                    msg = new ServerBinaryAckMessage<T>
                    {
                        Id = packetId,
                        Namespace = Namespace,
                        Json = result.Json
                    };
                    msg.OutgoingBytes = new List<byte[]>(result.Bytes);
                }
                else
                {
                    msg = new ServerAckMessage<T>
                    {
                        Namespace = Namespace,
                        Id = packetId,
                        Json = result.Json
                    };
                }
            }
            else
            {
                msg = new ServerAckMessage<T>
                {
                    Namespace = Namespace,
                    Id = packetId
                };
            }

            await Transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
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
                    var msg = new BinaryMessage<T>
                    {
                        Namespace = Namespace,
                        OutgoingBytes = new List<byte[]>(result.Bytes),
                        Event = eventName,
                        Json = result.Json
                    };
                    await Transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var msg = new EventMessage<T>
                    {
                        Namespace = Namespace,
                        Event = eventName,
                        Json = result.Json
                    };
                    await Transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                var msg = new EventMessage<T>
                {
                    Namespace = Namespace,
                    Event = eventName
                };
                await Transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Emits an event to the socket
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="ack">will be called with the server answer.</param>
        /// <param name="data">Any other parameters can be included. All serializable datastructures are supported, including byte[]</param>
        /// <returns></returns>
        public async Task EmitAsync(string eventName, Action<SocketIOResponse<T>> ack, params object[] data)
        {
            await EmitAsync(eventName, CancellationToken.None, ack, data).ConfigureAwait(false);
        }

        public async Task EmitAsync(string eventName,
            CancellationToken cancellationToken,
            Action<SocketIOResponse<T>> ack,
            params object[] data)
        {
            _ackHandlers.Add(++_packetId, ack);
            if (data != null && data.Length > 0)
            {
                var result = JsonSerializer.Serialize(data);
                if (result.Bytes.Count > 0)
                {
                    var msg = new ClientBinaryAckMessage<T>
                    {
                        Event = eventName,
                        Namespace = Namespace,
                        Json = result.Json,
                        Id = _packetId,
                        OutgoingBytes = new List<byte[]>(result.Bytes)
                    };
                    await Transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var msg = new ClientAckMessage<T>
                    {
                        Event = eventName,
                        Namespace = Namespace,
                        Id = _packetId,
                        Json = result.Json
                    };
                    await Transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                var msg = new ClientAckMessage<T>
                {
                    Event = eventName,
                    Namespace = Namespace,
                    Id = _packetId
                };
                await Transport.SendAsync(msg, cancellationToken).ConfigureAwait(false);
            }
        }


        private readonly SemaphoreSlim _disconnectingLock = new SemaphoreSlim(1, 1);

        private async Task InvokeDisconnect(string reason)
        {
            try
            {
                await _disconnectingLock.WaitAsync();
                if (!Connected)
                {
                    return;
                }

                try
                {
                    await Transport.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
#if DEBUG
                    Debug.WriteLine(e);
#endif
                }

                Connected = false;
                Id = null;
                OnDisconnected.TryInvoke(this, reason);
                if (reason != DisconnectReason.IOServerDisconnect && reason != DisconnectReason.IOClientDisconnect)
                {
                    //In the this cases (explicit disconnection), the client will not try to reconnect and you need to manually call socket.connect().
                    if (Options.Reconnection)
                    {
                        ConnectInBackground();
                    }
                }
            }
            finally
            {
                _disconnectingLock.Release();
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
            _connCts.TryDispose();
            Transport.TryDispose();
            _ackHandlers.Clear();
            _onAnyHandlers.Clear();
            _eventHandlers.Clear();
        }
    }

    /// <summary>
    /// socket.io client class with with JsonElement from System.Text.Json type selected as serializer input parameters and return types, if you want to choose other type use generic implementation of SocketIO
    /// </summary>
    public class SocketIO : SocketIO<JsonElement>
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
        public SocketIO(Uri uri, SocketIOOptions options) : base(uri, options, new SystemTextJsonSerializer())
        {

        }


    }

}