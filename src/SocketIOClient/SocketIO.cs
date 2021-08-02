using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.EioHandler;
using SocketIOClient.JsonSerializer;
using SocketIOClient.Processors;
using SocketIOClient.WebSocketClient;

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
        public SocketIO(string uri) : this(new Uri(uri)) { }

        /// <summary>
        /// Create SocketIO object with options
        /// </summary>
        /// <param name="uri"></param>
        public SocketIO(Uri uri) : this(uri, new SocketIOOptions()) { }

        /// <summary>
        /// Create SocketIO object with options
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="options"></param>
        public SocketIO(string uri, SocketIOOptions options) : this(new Uri(uri), options) { }

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

        /// <summary>
        /// Create SocketIO object with default options
        /// </summary>
        public SocketIO()
        {
            Options = new SocketIOOptions();
            Initialize();
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

        public UrlConverter UrlConverter { get; set; }

        IWebSocketClient _socket;
        public IWebSocketClient Socket
        {
            get => _socket;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException();
                }
                if (_socket != value)
                {
                    _socket = value;
                    value.OnTextReceived = OnTextReceived;
                    value.OnBinaryReceived = OnBinaryReceived;
                    value.OnClosed = OnClosed;
                    value.ConnectionTimeout = Options.ConnectionTimeout;
                }
            }
        }

        public Processor MessageProcessor { get; set; }

        /// <summary>
        /// An unique identifier for the socket session. Set after the connect event is triggered, and updated after the reconnect event.
        /// </summary>
        public string Id { get; set; }

        public string Namespace { get; private set; }

        /// <summary>
        /// Whether or not the socket is connected to the server.
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// Gets current attempt of reconnection.
        /// </summary>
        public int Attempts { get; private set; }

        /// <summary>
        /// Whether or not the socket is disconnected from the server.
        /// </summary>
        public bool Disconnected { get; private set; }

        public SocketIOOptions Options { get; }

        public IJsonSerializer JsonSerializer { get; set; }

        int _packetId;
        List<byte[]> _outGoingBytes;
        Queue<LowLevelEvent> _lowLevelEvents;
        Dictionary<int, Action<SocketIOResponse>> _ackHandlers;
        List<OnAnyHandler> _onAnyHandlers;
        Dictionary<string, Action<SocketIOResponse>> _eventHandlers;
        CancellationTokenSource _pingTokenSorce;
        CancellationTokenSource _connectionTokenSorce;
        DateTime _pingTime;
        int _pingInterval;
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

        private void Initialize()
        {
            UrlConverter = new UrlConverter();
            Socket = new WebSocketClient.ClientWebSocket();
            MessageProcessor = new EngineIOProtocolProcessor();
            _packetId = -1;
            _ackHandlers = new Dictionary<int, Action<SocketIOResponse>>();
            _eventHandlers = new Dictionary<string, Action<SocketIOResponse>>();
            _outGoingBytes = new List<byte[]>();
            _onAnyHandlers = new List<OnAnyHandler>();
            _lowLevelEvents = new Queue<LowLevelEvent>();

            Disconnected = true;
            JsonSerializer = new SystemTextJsonSerializer(Options.EIO);
            _connectionTokenSorce = new CancellationTokenSource();
        }

        public async Task ConnectAsync()
        {
            Uri wsUri = UrlConverter.HttpToWs(ServerUri, Options);
            while (true)
            {
                try
                {
                    if (_connectionTokenSorce.IsCancellationRequested)
                    {
                        break;
                    }
                    if (Attempts == 0)
                    {
                        _reconnectionDelay = Options.ReconnectionDelay;
                    }
                    else if (Attempts > 0)
                    {
                        OnReconnectAttempt?.Invoke(this, Attempts);
                    }
                    await Socket.ConnectAsync(wsUri);
                    break;
                }
                catch (SystemException e)
                {
                    if (e is TimeoutException || e is WebSocketException)
                    {
                        if (Attempts > 0)
                        {
                            OnReconnectError?.Invoke(this, e);
                        }
                        Attempts++;
                        if (Attempts <= Options.ReconnectionAttempts)
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
                            OnReconnectFailed?.Invoke(this, EventArgs.Empty);
                            break;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public async Task DisconnectAsync()
        {
            if (Connected && !Disconnected)
            {
                if (Options.EIO == 3)
                {
                    _pingTokenSorce.Cancel();
                }
                try
                {
                    await Socket.SendMessageAsync("41" + Namespace + ',');
                }
                catch (Exception ex) { Trace.WriteLine(ex.Message); }
                Connected = false;
                Disconnected = true;
                try
                {
                    await Socket.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    //For normal flow we invoke disconnect event after the connection has been closed
                    await InvokeDisconnectAsync("io client disconnect");
                }
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

        private async Task EmityCoreAsync(string eventName, int packetId, string data, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentException("Event name is invalid");
            }
            var builder = new StringBuilder();
            if (_outGoingBytes.Count > 0)
                builder.Append("45").Append(_outGoingBytes.Count).Append("-");
            else
                builder.Append("42");
            if (Namespace != null)
            {
                builder.Append(Namespace).Append(',');
            }
            if (packetId > -1)
            {
                builder.Append(packetId);
            }
            builder.Append("[\"").Append(eventName).Append("\"");
            if (!string.IsNullOrEmpty(data))
            {
                builder.Append(',').Append(data);
            }
            builder.Append(']');
            string message = builder.ToString();
            try
            {
                await Socket.SendMessageAsync(message, cancellationToken);
                if (_outGoingBytes.Count > 0)
                {
                    foreach (var item in _outGoingBytes)
                    {
                        await Socket.SendMessageAsync(item, cancellationToken);
                    }
                    _outGoingBytes.Clear();
                }
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, e.Message);
            }
        }

        internal async Task EmitCallbackAsync(int packetId, params object[] data)
        {
            string dataString = GetDataString(data);

            var builder = new StringBuilder();
            if (_outGoingBytes.Count > 0)
                builder.Append("46").Append(_outGoingBytes.Count).Append("-");
            else
                builder.Append("43");
            if (Namespace != null)
                builder.Append(Namespace).Append(',');
            builder
                .Append(packetId)
                .Append("[")
                .Append(dataString)
                .Append("]");
            string message = builder.ToString();
            await Socket.SendMessageAsync(message);
            if (_outGoingBytes.Count > 0)
            {
                foreach (var item in _outGoingBytes)
                {
                    await Socket.SendMessageAsync(item);
                }
                _outGoingBytes.Clear();
            }
        }

        private string GetDataString(params object[] data)
        {
            if (data != null && data.Length > 0)
            {
                var result = JsonSerializer.Serialize(data);
                if (result.Bytes.Count > 0)
                {
                    _outGoingBytes.AddRange(result.Bytes);
                }
                return result.Json.Substring(1, result.Json.Length - 2);
            }
            return string.Empty;
        }

        /// <summary>
        /// Emits an event to the socket
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="data">Any other parameters can be included. All serializable datastructures are supported, including byte[]</param>
        /// <returns></returns>
        public async Task EmitAsync(string eventName, params object[] data)
        {
            await EmitAsync(eventName, CancellationToken.None, data);
        }

        public async Task EmitAsync(string eventName, CancellationToken cancellationToken, params object[] data)
        {
            string dataString = GetDataString(data);
            await EmityCoreAsync(eventName, -1, dataString, cancellationToken);
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
            await EmitAsync(eventName, CancellationToken.None, ack, data);
        }

        public async Task EmitAsync(string eventName, CancellationToken cancellationToken, Action<SocketIOResponse> ack, params object[] data)
        {
            _ackHandlers.Add(++_packetId, ack);
            string dataString = GetDataString(data);
            await EmityCoreAsync(eventName, _packetId, dataString, cancellationToken);
        }

        private void OnTextReceived(string message)
        {
            MessageProcessor.Process(new MessageContext
            {
                Message = message,
                Namespace = Namespace,
                EioHandler = Options.EioHandler,
                OpenedHandler = OpenedHandler,
                ConnectedHandler = ConnectedHandler,
                AckHandler = AckHandler,
                BinaryAckHandler = BinaryAckHandler,
                BinaryReceivedHandler = BinaryReceivedHandler,
                DisconnectedHandler = DisconnectedHandler,
                ErrorHandler = ErrorHandler,
                EventReceivedHandler = EventReceivedHandler,
                PingHandler = PingHandler,
                PongHandler = PongHandler,
            });
        }

        private void OnBinaryReceived(byte[] bytes)
        {
            var buffer = Options.EioHandler.GetBytes(bytes);

            if (_lowLevelEvents.Count > 0)
            {
                var e = _lowLevelEvents.Peek();
                e.Response.InComingBytes.Add(buffer);
                if (e.Response.InComingBytes.Count == e.Count)
                {
                    if (e.PacketId == -1)
                    {
                        foreach (var item in _onAnyHandlers)
                        {
                            item(e.Event, e.Response);
                        }
                        _eventHandlers[e.Event](e.Response);
                    }
                    else
                    {
                        _ackHandlers[e.PacketId](e.Response);
                        _ackHandlers.Remove(e.PacketId);
                    }
                    _lowLevelEvents.Dequeue();
                }
            }
        }

        private async void OnClosed(string reason)
        {
            await InvokeDisconnectAsync(reason);
        }

        private async void OpenedHandler(string sid, int pingInterval, int pingTimeout)
        {
            Id = sid;
            _pingInterval = pingInterval;
            string msg = Options.EioHandler.CreateConnectionMessage(Namespace, Options.Query);
            await Socket.SendMessageAsync(msg);
        }

        private void ConnectedHandler(ConnectionResult result)
        {
            if (result.Result)
            {
                Connected = true;
                Disconnected = false;
                OnReconnected?.Invoke(this, Attempts);
                Attempts = 0;

                if (result.Id != null)
                {
                    Id = result.Id;
                }
                OnConnected?.Invoke(this, EventArgs.Empty);
                if (Options.EIO == 3)
                {
                    _pingTokenSorce = new CancellationTokenSource();
                    _ = StartPingAsync();
                }
            }
        }

        private void AckHandler(int packetId, List<JsonElement> array)
        {
            if (_ackHandlers.ContainsKey(packetId))
            {
                var response = new SocketIOResponse(array, this);
                _ackHandlers[packetId](response);
                _ackHandlers.Remove(packetId);
            }
        }

        private void BinaryAckHandler(int packetId, int totalCount, List<JsonElement> array)
        {
            if (_ackHandlers.ContainsKey(packetId))
            {
                var response = new SocketIOResponse(array, this);
                _lowLevelEvents.Enqueue(new LowLevelEvent
                {
                    PacketId = packetId,
                    Count = totalCount,
                    Response = response
                });
            }
        }

        private void BinaryReceivedHandler(int packetId, int totalCount, string eventName, List<JsonElement> array)
        {
            var response = new SocketIOResponse(array, this)
            {
                PacketId = packetId
            };
            _lowLevelEvents.Enqueue(new LowLevelEvent
            {
                Event = eventName,
                Count = totalCount,
                Response = response
            });
        }

        private async void DisconnectedHandler()
        {
            await InvokeDisconnectAsync("io server disconnect");
        }

        private void ErrorHandler(string error)
        {
            OnError?.Invoke(this, error);
        }

        private void EventReceivedHandler(int packetId, string eventName, List<JsonElement> array)
        {
            var response = new SocketIOResponse(array, this)
            {
                PacketId = packetId
            };
            foreach (var item in _onAnyHandlers)
            {
                item(eventName, response);
            }
            if (_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName](response);
            }
        }

        public async void PingHandler()
        {
            try
            {
                OnPing?.Invoke(this, EventArgs.Empty);
                DateTime pingTime = DateTime.Now;
                await Socket.SendMessageAsync("3");
                OnPong?.Invoke(this, DateTime.Now - pingTime);
            }
            catch (WebSocketException e)
            {
                await InvokeDisconnectAsync(e.Message);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                OnError?.Invoke(this, ex.Message);
            }
        }

        public void PongHandler()
        {
            OnPong?.Invoke(this, DateTime.Now - _pingTime);
        }

        private async Task InvokeDisconnectAsync(string reason)
        {
            if (Connected)
            {
                Connected = false;
                Disconnected = true;
                if (Options.EIO == 3)
                {
                    _pingTokenSorce.Cancel();
                }
                OnDisconnected?.Invoke(this, reason);
                if (reason != "io server disconnect" && reason != "io client disconnect")
                {
                    //In the this cases (explicit disconnection), the client will not try to reconnect and you need to manually call socket.connect().
                    if (Options.Reconnection)
                    {
                        await ConnectAsync();
                    }
                }
            }
        }

        public async Task StartPingAsync()
        {
            while (!_pingTokenSorce.IsCancellationRequested)
            {
                await Task.Delay(_pingInterval);
                if (Connected)
                {
                    try
                    {
                        _pingTime = DateTime.Now;
                        await Socket.SendMessageAsync("2");
                        OnPing?.Invoke(this, EventArgs.Empty);
                    }
                    catch (WebSocketException e)
                    {
                        await InvokeDisconnectAsync(e.Message);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                        OnError?.Invoke(this, ex.Message);
                    }
                }
                else
                {
                    StopPingInterval();
                }
            }
        }

        public void StopPingInterval()
        {
            _pingTokenSorce?.Cancel();
        }

        public void Dispose()
        {
            Socket.Dispose();
            _outGoingBytes.Clear();
            _ackHandlers.Clear();
            _onAnyHandlers.Clear();
            _eventHandlers.Clear();
            _lowLevelEvents.Clear();
            _connectionTokenSorce.Cancel();
            _connectionTokenSorce.Dispose();
            if (_pingTokenSorce != null)
            {
                _pingTokenSorce.Cancel();
                _pingTokenSorce.Dispose();
            }
        }
    }
}