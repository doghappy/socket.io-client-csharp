using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.ConnectInterval;
using SocketIOClient.EioHandler;
using SocketIOClient.Exceptions;
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
        DateTime _pingTime;
        int _pingInterval;

        #region Socket.IO event
        public event EventHandler OnConnected;
        public event EventHandler<string> OnConnectError;
        public event EventHandler<string> OnConnectTimeout;
        public event EventHandler<string> OnError;
        public event EventHandler<string> OnDisconnected;
        public event EventHandler<int> OnReconnectAttempt;
        [Obsolete]
        public event EventHandler<int> OnReconnecting;
        public event EventHandler<string> OnReconnectError;
        public event EventHandler<Exception> OnReconnectFailed;
        public event EventHandler OnPing;
        public event EventHandler<TimeSpan> OnPong;

        #endregion

        private void Initialize()
        {
            UrlConverter = new UrlConverter();
            Socket = new DefaultClient();
            MessageProcessor = new EngineIOProtocolProcessor();
            _packetId = -1;
            _ackHandlers = new Dictionary<int, Action<SocketIOResponse>>();
            _eventHandlers = new Dictionary<string, Action<SocketIOResponse>>();
            _outGoingBytes = new List<byte[]>();
            _onAnyHandlers = new List<OnAnyHandler>();
            _lowLevelEvents = new Queue<LowLevelEvent>();

            Disconnected = true;
            OnDisconnected += SocketIO_OnDisconnected;
            JsonSerializer = new SystemTextJsonSerializer(Options.EIO);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync()
        {
            if (ServerUri == null)
            {
                throw new ArgumentException(nameof(ServerUri));
            }

            Uri wsUri = UrlConverter.HttpToWs(ServerUri, Options);

            try
            {
                await Socket.ConnectAsync(wsUri);
            }
            catch (SystemException ex)
            {
                if (ex is TimeoutException || ex is WebSocketException)
                {
                    if (Options.AllowedRetryFirstConnection || Options.Reconnection)
                    {
                        await this.ReconnectAsync();
                    }
                    else 
                    {
                        OnConnectTimeout?.Invoke(this, ex.Message);
                    }
                }
                else 
                {
                    OnConnectError?.Invoke(this, ex.Message);
                }
            }
            catch (Exception ex)
            {
                OnConnectError?.Invoke(this, ex.Message);
            }
        }

        private async Task ReconnectAsync()
        {
            this.Attempts = 0;
            Uri wsUri = UrlConverter.HttpToWs(ServerUri, Options);
            var connectInterval = new DefaultConnectInterval(Options);

            while (true)
            {
                try
                {
                    this.Attempts++;

                    // If current delay is already bigger than ReconnectionDelayMax, we just take ReconnectionDelayMax.
                    double delay = connectInterval.GetDelay() < Options.ReconnectionDelayMax ? connectInterval.NextDelay() : Options.ReconnectionDelayMax;

                    // Take the minimum value between delay and ReconnectionDelayMax.
                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(delay, Options.ReconnectionDelayMax)));

                    this.OnReconnectAttempt?.Invoke(this, Attempts);
                    this.OnReconnecting?.Invoke(this, Attempts);
                    await Socket.ConnectAsync(wsUri);
                    break;
                }
                catch (SystemException ex)
                {
                    if (ex is TimeoutException || ex is WebSocketException)
                    {
                        if (Attempts >= Options.ReconnectionAttempts)
                        {
                            OnReconnectFailed?.Invoke(this, ex);
                            break;
                        }
                    }
                    else
                    {
                        this.OnReconnectError?.Invoke(this, ex.Message);
                        break;
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
                catch (Exception ex) { Trace.WriteLine(ex.Message); }
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
            catch (WebSocketException e)
            {
                InvokeDisconnect(e.Message);
            }
            catch (InvalidSocketStateException e)
            {
                InvokeDisconnect(e.Message);
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

        private void OnClosed(string reason)
        {
            if (reason != null)
            {
                InvokeDisconnect(reason);
            }
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
                if (result.Id != null)
                {
                    Id = result.Id;
                }
                OnConnected?.Invoke(this, new EventArgs());
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

        private void DisconnectedHandler()
        {
            InvokeDisconnect("io server disconnect");
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
                OnPing?.Invoke(this, new EventArgs());
                DateTime pingTime = DateTime.Now;
                await Socket.SendMessageAsync("3");
                OnPong?.Invoke(this, DateTime.Now - pingTime);
            }
            catch (WebSocketException e)
            {
                InvokeDisconnect(e.Message);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        public void PongHandler()
        {
            OnPong?.Invoke(this, DateTime.Now - _pingTime);
        }

        private void InvokeDisconnect(string reason)
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
            }
        }

        private async void SocketIO_OnDisconnected(object sender, string reason)
        {
            // The reconnection should be performed if option was set and if the reason of the disconnection is unexpected.
            if (Options.Reconnection && this.IsUnexpectedDisconnection(reason))
            {
                await ReconnectAsync();
            }
        }

        private bool IsUnexpectedDisconnection(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return true;
            }
            
            // We check if the server has forcefully disconnected the socket with socket.disconnect()
            // or if the the socket was manually disconnected by the client using socket.disconnect()
            return !(reason.Equals("io server disconnect") || reason.Equals("io client disconnect"));
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
                        OnPing?.Invoke(this, new EventArgs());
                    }
                    catch (WebSocketException e)
                    {
                        InvokeDisconnect(e.Message);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
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
        }
    }
}
