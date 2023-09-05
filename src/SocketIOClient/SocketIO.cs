using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SocketIO.Core;
using SocketIO.Serializer.Core;
using SocketIO.Serializer.SystemTextJson;
using SocketIOClient.Extensions;
using SocketIOClient.Transport;
using SocketIOClient.Transport.Http;
using SocketIOClient.Transport.WebSockets;
using SocketIOClient.UriConverters;

[assembly: InternalsVisibleTo("SocketIOClient.UnitTests, PublicKey=002400000480000094000000060200000024" +
                              "0000525341310004000001000100b18b07d8d9f5f79927b53fb9601562a4986cd90fd64cbb7ccf0bd258" +
                              "dc3f2119a2c7db7bfea28eba76ae4346a125e56a6b6713e920656c598027182f19b54bcd9f9012228b51" +
                              "93d84c565e54caee24e4fcfa6f0cbe611b4bc631578fb4aa5f7dabf5beacbe8df27716a5a1849b5d124e" +
                              "5924161577424002142ba1ade29d089c")]

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

        public ISerializer Serializer { get; set; }
        public ITransport Transport { get; set; }
        public IHttpClient HttpClient { get; set; }

        public Func<IClientWebSocket> ClientWebSocketProvider { get; set; }

        List<IDisposable> _resources = new List<IDisposable>();

        List<Type> _expectedExceptions;

        int _packetId;
        Exception _backgroundException;
        Dictionary<int, Action<SocketIOResponse>> _ackActionHandlers;
        internal Dictionary<int, Action<SocketIOResponse>> AckActionHandlers => _ackActionHandlers;
        Dictionary<int, Func<SocketIOResponse, Task>> _ackFuncHandlers;
        internal Dictionary<int, Func<SocketIOResponse, Task>> AckFuncHandlers => _ackFuncHandlers;
        List<OnAnyHandler> _onAnyHandlers;
        private Dictionary<string, Action<SocketIOResponse>> _eventActionHandlers;
        private Dictionary<string, Func<SocketIOResponse, Task>> _eventFuncHandlers;
        double _reconnectionDelay;
        bool _exitFromBackground;
        readonly SemaphoreSlim _packetIdLock = new(1, 1);

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

        private void Initialize()
        {
            _packetId = -1;
            _ackActionHandlers = new Dictionary<int, Action<SocketIOResponse>>();
            _ackFuncHandlers = new Dictionary<int, Func<SocketIOResponse, Task>>();
            _eventActionHandlers = new Dictionary<string, Action<SocketIOResponse>>();
            _eventFuncHandlers = new Dictionary<string, Func<SocketIOResponse, Task>>();
            _onAnyHandlers = new List<OnAnyHandler>();

            Serializer = new SystemTextJsonSerializer();

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
                Auth = Options.Auth,
                ConnectionTimeout = Options.ConnectionTimeout
            };
            if (Options.Transport == TransportProtocol.Polling)
            {
                var handler = HttpPollingHandler.CreateHandler(transportOptions.EIO, HttpClient);
                Transport = new HttpTransport(transportOptions, handler, Serializer);
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
                Transport = new WebSocketTransport(transportOptions, ws, Serializer);
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
                    var serverUri = UriConverter.GetServerUri(
                        Options.Transport == TransportProtocol.WebSocket,
                        ServerUri,
                        Options.EIO,
                        Options.Path,
                        Options.Query);
                    if (_attempts > 0)
                        OnReconnectAttempt.TryInvoke(this, _attempts);
                    try
                    {
                        using var cts = new CancellationTokenSource(Options.ConnectionTimeout);
                        await Transport.ConnectAsync(serverUri, cts.Token).ConfigureAwait(false);
                        break;
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
                    Debug.WriteLine(e);
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

        private void PongHandler(IMessage msg)
        {
            OnPong.TryInvoke(this, msg.Duration);
        }

        private void ConnectedHandler(IMessage msg)
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

        private void EventMessageHandler(IMessage message)
        {
            // TODO: run actions in background. eg: Task.Run
            var res = new SocketIOResponse(message, this)
            {
                PacketId = message.Id
            };
            foreach (var item in _onAnyHandlers)
            {
                item.TryInvoke(message.Event, res);
            }

            if (_eventActionHandlers.TryGetValue(message.Event, out var actionHandler))
            {
                actionHandler.TryInvoke(res);
            }
            else if (_eventFuncHandlers.TryGetValue(message.Event, out var funcHandler))
            {
                funcHandler.TryInvoke(res);
            }
        }

        private void AckMessageHandler(IMessage message)
        {
            var res = new SocketIOResponse(message, this);
            if (_ackActionHandlers.ContainsKey(message.Id))
            {
                _ackActionHandlers[message.Id].TryInvoke(res);
                _ackActionHandlers.Remove(message.Id);
            }
            else if (_ackFuncHandlers.ContainsKey(message.Id))
            {
                _ackFuncHandlers[message.Id].TryInvoke(res);
                _ackFuncHandlers.Remove(message.Id);
            }
        }

        private void ErrorMessageHandler(IMessage msg)
        {
            _connCts.Dispose();
            OnError.TryInvoke(this, msg.Error);
        }

        private void BinaryMessageHandler(IMessage message)
        {
            var response = new SocketIOResponse(message, this)
            {
                PacketId = message.Id,
            };
            foreach (var item in _onAnyHandlers)
            {
                // TODO: run in background to make sure user logic could not block socket.io's logic
                item.TryInvoke(message.Event, response);
            }

            if (_eventActionHandlers.TryGetValue(message.Event, out var handler))
            {
                handler.TryInvoke(response);
            }
        }

        private void BinaryAckMessageHandler(IMessage msg)
        {
            if (_ackActionHandlers.TryGetValue(msg.Id, out var handler))
            {
                var response = new SocketIOResponse(msg, this)
                {
                    PacketId = msg.Id,
                };
                handler.TryInvoke(response);
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
                        PongHandler(msg);
                        break;
                    case MessageType.Connected:
                        ConnectedHandler(msg);
                        break;
                    case MessageType.Disconnected:
                        DisconnectedHandler();
                        break;
                    case MessageType.Event:
                        EventMessageHandler(msg);
                        break;
                    case MessageType.Ack:
                        AckMessageHandler(msg);
                        break;
                    case MessageType.Error:
                        ErrorMessageHandler(msg);
                        break;
                    case MessageType.Binary:
                        BinaryMessageHandler(msg);
                        break;
                    case MessageType.BinaryAck:
                        BinaryAckMessageHandler(msg);
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
            // try
            // {
            //     var message = Serializer.SerializeDisconnectionMessage();
            //     using var cts = new CancellationTokenSource(Options.ConnectionTimeout);
            //     await Transport.SendAsync(new List<SerializedItem> { message }, cts.Token).ConfigureAwait(false);
            // }
            // catch (Exception e)
            // {
            //     Debug.WriteLine(e);
            // }

            await InvokeDisconnect(DisconnectReason.IOClientDisconnect);
        }

        /// <summary>
        /// Register a new handler for the given event.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="callback"></param>
        public void On(string eventName, Action<SocketIOResponse> callback)
        {
            if (_eventActionHandlers.ContainsKey(eventName))
            {
                _eventActionHandlers.Remove(eventName);
            }

            _eventActionHandlers.Add(eventName, callback);
        }

        public void On(string eventName, Func<SocketIOResponse, Task> callback)
        {
            if (_eventFuncHandlers.ContainsKey(eventName))
            {
                _eventFuncHandlers.Remove(eventName);
            }

            _eventFuncHandlers.Add(eventName, callback);
        }


        /// <summary>
        /// Unregister a new handler for the given event.
        /// </summary>
        /// <param name="eventName"></param>
        public void Off(string eventName)
        {
            if (_eventActionHandlers.ContainsKey(eventName))
            {
                _eventActionHandlers.Remove(eventName);
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
            var serializedItems = Serializer.Serialize(Options.EIO, packetId, Namespace, data);
            await Transport.SendAsync(serializedItems, cancellationToken).ConfigureAwait(false);
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

        private async Task EmitAsync(string eventName, CancellationToken cancellationToken, params object[] data)
        {
            var serializedItems = Serializer.Serialize(Options.EIO, eventName, Namespace, data);
            await Transport.SendAsync(serializedItems, cancellationToken).ConfigureAwait(false);
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

        public async Task EmitAsync(string eventName, Func<SocketIOResponse, Task> ack, params object[] data)
        {
            await EmitAsync(eventName, CancellationToken.None, ack, data).ConfigureAwait(false);
        }

        private async Task EmitForAck(string eventName, int packetId, CancellationToken cancellationToken,
            params object[] data)
        {
            var serializedItems = Serializer.Serialize(Options.EIO, eventName, packetId, Namespace, data);
            await Transport.SendAsync(serializedItems, cancellationToken).ConfigureAwait(false);
        }

        private async Task EmitAsync(
            string eventName,
            CancellationToken cancellationToken,
            Action<SocketIOResponse> ack,
            params object[] data)
        {
            try
            {
                await _packetIdLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                _ackActionHandlers.Add(++_packetId, ack);
                await EmitForAck(eventName, _packetId, cancellationToken, data).ConfigureAwait(false);
            }
            finally
            {
                _packetIdLock.Release();
            }
        }

        private async Task EmitAsync(
            string eventName,
            CancellationToken cancellationToken,
            Func<SocketIOResponse, Task> ack,
            params object[] data)
        {
            try
            {
                await _packetIdLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                _ackFuncHandlers.Add(++_packetId, ack);
                await EmitForAck(eventName, _packetId, cancellationToken, data).ConfigureAwait(false);
            }
            finally
            {
                _packetIdLock.Release();
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
            _ackActionHandlers.Clear();
            _onAnyHandlers.Clear();
            _eventActionHandlers.Clear();
        }
    }
}
