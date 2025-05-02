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
        private ITransport Transport { get; set; }
        public IHttpClient HttpClient { get; set; }

        public Func<IClientWebSocket> ClientWebSocketProvider { get; set; }

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
        readonly SemaphoreSlim _packetIdLock = new(1, 1);
        private TaskCompletionSource<bool> _openedCompletionSource = new();
        private TaskCompletionSource<bool> _transportCompletionSource = new();
        private TaskCompletionSource<Exception> _connBackgroundSource = new();

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


        private void InitializeStatus()
        {
            Id = null;
            Connected = false;
            _backgroundException = null;
            _reconnectionDelay = Options.ReconnectionDelay;
            Transport?.Dispose();
        }

        private void Initialize()
        {
            _packetId = -1;
            _ackActionHandlers = new Dictionary<int, Action<SocketIOResponse>>();
            _ackFuncHandlers = new Dictionary<int, Func<SocketIOResponse, Task>>();
            _eventActionHandlers = new Dictionary<string, Action<SocketIOResponse>>();
            _eventFuncHandlers = new Dictionary<string, Func<SocketIOResponse, Task>>();
            _onAnyHandlers = new List<OnAnyHandler>();

            Serializer = new SystemTextJsonSerializer();

            HttpClient = new DefaultHttpClient(Options.RemoteCertificateValidationCallback);
            ClientWebSocketProvider = () =>
            {
                var ws = new DefaultClientWebSocket(Options.RemoteCertificateValidationCallback);
                return ws;
            };
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

        private ITransport NewTransport(TransportProtocol protocol, TransportOptions options)
        {
            ITransport transport = protocol switch
            {
                TransportProtocol.Polling => NewHttpTransport(options),
                TransportProtocol.WebSocket => NewWebSocketTransport(options),
                _ => throw new ArgumentOutOfRangeException()
            };

            OnTransportCreated(transport);
            return transport;
        }

        private TransportOptions NewTransportOptions()
        {
            return new TransportOptions
            {
                EIO = Options.EIO,
                Query = Options.Query ?? new List<KeyValuePair<string, string>>(),
                Auth = Options.Auth,
                ServerUri = ServerUri,
                Path = Options.Path,
                ConnectionTimeout = Options.ConnectionTimeout,
                AutoUpgrade = Options.AutoUpgrade,
                Namespace = Namespace
            };
        }

        private HttpTransport NewHttpTransport(TransportOptions options)
        {
            var handler = HttpPollingHandler.CreateHandler(options.EIO, HttpClient);
            var transport = new HttpTransport(options, handler, Serializer);
            SetHttpHeaders();
            return transport;
        }

        private WebSocketTransport NewWebSocketTransport(TransportOptions options)
        {
            var ws = ClientWebSocketProvider();
            if (ws is null)
            {
                throw new ArgumentNullException(nameof(ClientWebSocketProvider),
                    $"{ClientWebSocketProvider} returns a null");
            }

            var transport = new WebSocketTransport(options, ws, Serializer);
            SetWebSocketHeaders(transport);
            return transport;
        }

        private void OnTransportCreated(ITransport transport)
        {
            if (Options.Proxy != null)
            {
                transport.SetProxy(Options.Proxy);
            }

            transport.OnReceived = OnMessageReceived;
            transport.OnError = OnErrorReceived;
        }

        private void SetWebSocketHeaders(ITransport transport)
        {
            if (Options.ExtraHeaders is null)
            {
                return;
            }

            foreach (var item in Options.ExtraHeaders)
            {
                transport.AddHeader(item.Key, item.Value);
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

        private void ConnectInBackground(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                InitializeStatus();
                while (!cancellationToken.IsCancellationRequested)
                {
                    var options = NewTransportOptions();
                    var transport = NewTransport(Options.Transport, options);
                    if (_attempts > 0)
                        OnReconnectAttempt.TryInvoke(this, _attempts);
                    try
                    {
                        await transport.ConnectAsync(cancellationToken).ConfigureAwait(false);
                        Transport = transport;
                        _transportCompletionSource.SetResult(true);
                        break;
                    }
                    catch (Exception e)
                    {
                        transport.Dispose();
                        OnReconnectError.TryInvoke(this, e);
                        var failedToAttempt = await AttemptAsync();
                        if (failedToAttempt)
                        {
                            _connBackgroundSource.SetResult(
                                new ConnectionException($"Cannot connect to server '{ServerUri}'."));
                            break;
                        }

                        var canHandle = CanHandleException(e);
                        if (canHandle) continue;
                        _connBackgroundSource.SetResult(
                            new ConnectionException($"Cannot connect to server '{ServerUri}'", e));
                        throw;
                    }
                }
            }, cancellationToken);
        }

        private async Task UpgradeToWebSocket(IMessage openedMessage)
        {
            var options = NewTransportOptions();
            options.OpenedMessage = openedMessage;

            for (var i = 0; i < 3; i++)
            {
                WebSocketTransport transport = (WebSocketTransport)NewTransport(TransportProtocol.WebSocket, options);

                TaskCompletionSource<bool> pongProbeTcs = new();
                using CancellationTokenSource connectionTimeoutCts = new(Options.ConnectionTimeout);
                CancellationToken connectionTimeoutToken = connectionTimeoutCts.Token;

                connectionTimeoutToken.Register(() =>
                {
                    pongProbeTcs.TrySetException(new TimeoutException("The upgrade operation has timed out!"));
                });

                try
                {
                    await transport.ConnectAsync(connectionTimeoutToken).ConfigureAwait(false);

                    void pongProbeHandler(IMessage msg)
                    {
                        if (msg.Type == MessageType.Pong && msg.ReceivedText == "probe")
                        {
                            pongProbeTcs.SetResult(true);
                        }
                        else
                        {
                            pongProbeTcs.SetException(new Exception($"Unexpected handshake response: '{msg.Type}'"));
                        }
                    }

                    transport.OnReceived += pongProbeHandler;

                    SerializedItem message = Serializer.SerializePingProbeMessage();

                    await transport
                        .SendAsync(new List<SerializedItem> { message }, connectionTimeoutToken)
                        .ConfigureAwait(false);

                    await pongProbeTcs.Task;

                    transport.OnReceived -= pongProbeHandler;

                    message = Serializer.SerializeUpgradeMessage();
                    await transport
                        .SendAsync(new List<SerializedItem> { message }, connectionTimeoutToken)
                        .ConfigureAwait(false);

                    Transport.Dispose();
                    Transport = transport;
                    Options.Transport = TransportProtocol.WebSocket;
                    transport.OnUpgraded();
                    break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    transport.Dispose();
                    var ex = new ConnectionException("Upgrade to websocket failed", e);
                    OnReconnectError.TryInvoke(this, ex);
                }
            }

            _openedCompletionSource.SetResult(true);
        }

        private async Task<bool> AttemptAsync()
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

        private readonly SemaphoreSlim _connectingLock = new(1, 1);
        private CancellationTokenSource _connCts = new();

        private async Task ConnectCore(CancellationToken cancellationToken)
        {
            await _connectingLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Connected) return;

                ConnectInBackground(cancellationToken);
                cancellationToken.Register(_connBackgroundSource.SetCanceled);
                var task = Task
                    .Run(async () => await _connBackgroundSource.Task.ConfigureAwait(false), cancellationToken);

                var ex = await task.ConfigureAwait(false);
                _connBackgroundSource = new TaskCompletionSource<Exception>();
                if (ex is not null)
                {
                    throw ex;
                }
            }
            finally
            {
                _connectingLock.Release();
            }
        }

        public async Task ConnectAsync()
        {
            await ConnectCore(_connCts.Token).ConfigureAwait(false);
            _connCts.Dispose();
            _connCts = new CancellationTokenSource();
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            await ConnectCore(cancellationToken).ConfigureAwait(false);
        }

        private void PingHandler()
        {
            OnPing.TryInvoke(this, EventArgs.Empty);
        }

        private void PongHandler(IMessage msg)
        {
            OnPong.TryInvoke(this, msg.Duration);
        }

        private async Task OpenedHandler(IMessage msg)
        {
            await _transportCompletionSource.Task;
            _transportCompletionSource = new TaskCompletionSource<bool>();
            if (Options.AutoUpgrade
                && Options.Transport == TransportProtocol.Polling
                && msg.Upgrades.Contains("websocket"))
            {
                _ = UpgradeToWebSocket(msg);
                return;
            }

            _openedCompletionSource.SetResult(true);
        }


        private async Task ConnectedHandler(IMessage msg)
        {
            await _openedCompletionSource.Task;
            _openedCompletionSource = new TaskCompletionSource<bool>();

            Id = msg.Sid;
            Connected = true;

            OnConnected.TryInvoke(this, EventArgs.Empty);
            if (_attempts > 0)
            {
                OnReconnected.TryInvoke(this, _attempts);
            }

            _attempts = 0;
            _connBackgroundSource.SetResult(null);
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
                    case MessageType.Opened:
                        _ = OpenedHandler(msg);
                        break;
                    case MessageType.Connected:
                        _ = ConnectedHandler(msg);
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

            if (_eventFuncHandlers.ContainsKey(eventName))
            {
                _eventFuncHandlers.Remove(eventName);
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
                        ConnectInBackground(_connCts.Token);
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