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
        internal int PacketId { get; private set; }
        internal List<byte[]> OutGoingBytes { get; private set; }
        internal Dictionary<int, Action<SocketIOResponse>> Acks { get; private set; }
        internal List<OnAnyHandler> OnAnyHandlers { get; private set; }
        public SocketIOOptions Options { get; }

        internal Dictionary<string, Action<SocketIOResponse>> Handlers { get; set; }

        public Func<IConnectInterval> GetConnectInterval { get; set; }

        public IJsonSerializer JsonSerializer { get; set; }

        static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        CancellationTokenSource _pingTokenSorce;
        DateTime _pingTime;
        int _pingInterval;

        #region Socket.IO event
        public event EventHandler OnConnected;
        //public event EventHandler<string> OnConnectError;
        //public event EventHandler<string> OnConnectTimeout;
        public event EventHandler<string> OnError;
        public event EventHandler<string> OnDisconnected;
        //public event EventHandler<string> OnReconnectAttempt;
        public event EventHandler<int> OnReconnecting;
        //public event EventHandler<string> OnReconnectError;
        public event EventHandler<Exception> OnReconnectFailed;
        public event EventHandler OnPing;
        public event EventHandler<TimeSpan> OnPong;

        #endregion

        internal Queue<BelowNormalEvent> BelowNormalEvents { get; private set; }

        private void Initialize()
        {
            UrlConverter = new UrlConverter();
            Socket = new DefaultClient();
            MessageProcessor = new EngineIOProtocolProcessor();
            PacketId = -1;
            Acks = new Dictionary<int, Action<SocketIOResponse>>();
            Handlers = new Dictionary<string, Action<SocketIOResponse>>();
            OutGoingBytes = new List<byte[]>();
            OnAnyHandlers = new List<OnAnyHandler>();
            BelowNormalEvents = new Queue<BelowNormalEvent>();

            Disconnected = true;
            OnDisconnected += SocketIO_OnDisconnected;
            GetConnectInterval = () => new DefaultConnectInterval(Options);
            JsonSerializer = new SystemTextJsonSerializer(Options.EIO);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync()
        {
            await ConnectCoreAsync(Options.AllowedRetryFirstConnection);
        }

        private async Task ConnectCoreAsync(bool allowedRetryFirstConnection)
        {
            if (ServerUri == null)
            {
                throw new ArgumentException("Invalid ServerUri");
            }
            Uri wsUri = UrlConverter.HttpToWs(ServerUri, Options);
            if (allowedRetryFirstConnection)
            {
                var connectInterval = GetConnectInterval();
                while (true)
                {
                    try
                    {
                        this.OnReconnecting?.Invoke(this, ++Attempts);
                        await Socket.ConnectAsync(wsUri);
                        break;
                    }
                    catch (SystemException ex)
                    {
                        if (ex is TimeoutException || ex is System.Net.WebSockets.WebSocketException)
                        {
                            if (Attempts >= Options.ReconnectionAttempts)
                            {
                                OnReconnectFailed?.Invoke(this, ex);
                                break;
                            }
                            else
                            {
                                // If current delay is already bigger than ReconnectionDelayMax, we just take ReconnectionDelayMax.
                                double delay = connectInterval.GetDelay() < Options.ReconnectionDelayMax ? connectInterval.NextDelay() : Options.ReconnectionDelayMax;

                                // Take the minimun value between delay and ReconnectionDelayMax.
                                await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(delay, Options.ReconnectionDelayMax)));
                            }
                        }
                        else
                        {
                            Attempts = 0;
                            throw;
                        }
                    }
                }
            }
            else
            {
                await Socket.ConnectAsync(wsUri);
            }

            if (Connected)
            {
                Attempts = 0;
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
            if (Handlers.ContainsKey(eventName))
            {
                Handlers.Remove(eventName);
            }
            Handlers.Add(eventName, callback);
        }

        /// <summary>
        /// Unregister a new handler for the given event.
        /// </summary>
        /// <param name="eventName"></param>
        public void Off(string eventName)
        {
            if (Handlers.ContainsKey(eventName))
            {
                Handlers.Remove(eventName);
            }
        }

        public void OnAny(OnAnyHandler handler)
        {
            if (handler != null)
            {
                OnAnyHandlers.Add(handler);
            }
        }

        public void PrependAny(OnAnyHandler handler)
        {
            if (handler != null)
            {
                OnAnyHandlers.Insert(0, handler);
            }
        }

        public void OffAny(OnAnyHandler handler)
        {
            if (handler != null)
            {
                OnAnyHandlers.Remove(handler);
            }
        }

        public OnAnyHandler[] ListenersAny() => OnAnyHandlers.ToArray();

        private async Task EmityCoreAsync(string eventName, int packetId, string data, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentException("Event name is invalid");
            }
            var builder = new StringBuilder();
            if (OutGoingBytes.Count > 0)
                builder.Append("45").Append(OutGoingBytes.Count).Append("-");
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
                if (OutGoingBytes.Count > 0)
                {
                    foreach (var item in OutGoingBytes)
                    {
                        await Socket.SendMessageAsync(item, cancellationToken);
                    }
                    OutGoingBytes.Clear();
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
            if (OutGoingBytes.Count > 0)
                builder.Append("46").Append(OutGoingBytes.Count).Append("-");
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
            if (OutGoingBytes.Count > 0)
            {
                foreach (var item in OutGoingBytes)
                {
                    await Socket.SendMessageAsync(item);
                }
                OutGoingBytes.Clear();
            }
        }

        private string GetDataString(params object[] data)
        {
            if (data != null && data.Length > 0)
            {
                var result = JsonSerializer.Serialize(data);
                if (result.Bytes.Count > 0)
                {
                    OutGoingBytes.AddRange(result.Bytes);
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
            await _semaphoreSlim.WaitAsync();
            try
            {
                string dataString = GetDataString(data);
                await EmityCoreAsync(eventName, -1, dataString, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
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
            await EmitAsync(eventName, CancellationToken.None, ack, data);
        }

        public async Task EmitAsync(string eventName, CancellationToken cancellationToken, Action<SocketIOResponse> ack, params object[] data)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                Acks.Add(++PacketId, ack);
                string dataString = GetDataString(data);
                await EmityCoreAsync(eventName, PacketId, dataString, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private void OnTextReceived(string message)
        {
            if (message.StartsWith("42"))
            {

            }
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
            byte[] buffer;
            if (Options.EIO == 3)
            {
                buffer = new byte[bytes.Length - 1];
                Buffer.BlockCopy(bytes, 1, buffer, 0, buffer.Length);
            }
            else
            {
                buffer = bytes;
            }
            InvokeBytesReceived(buffer);
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
            if (Acks.ContainsKey(packetId))
            {
                var response = new SocketIOResponse(array, this);
                Acks[packetId](response);
                Acks.Remove(packetId);
            }
        }

        private void BinaryAckHandler(int packetId, int totalCount, List<JsonElement> array)
        {
            if (Acks.ContainsKey(packetId))
            {
                var response = new SocketIOResponse(array, this);
                BelowNormalEvents.Enqueue(new BelowNormalEvent
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
            BelowNormalEvents.Enqueue(new BelowNormalEvent
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
            foreach (var item in OnAnyHandlers)
            {
                item(eventName, response);
            }
            if (Handlers.ContainsKey(eventName))
            {
                Handlers[eventName](response);
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

        private void InvokeBytesReceived(byte[] bytes)
        {
            if (BelowNormalEvents.Count > 0)
            {
                var e = BelowNormalEvents.Peek();
                e.Response.InComingBytes.Add(bytes);
                if (e.Response.InComingBytes.Count == e.Count)
                {
                    if (e.PacketId == -1)
                    {
                        foreach (var item in OnAnyHandlers)
                        {
                            item(e.Event, e.Response);
                        }
                        Handlers[e.Event](e.Response);
                    }
                    else
                    {
                        Acks[e.PacketId](e.Response);
                        Acks.Remove(e.PacketId);
                    }
                    BelowNormalEvents.Dequeue();
                }
            }
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

        private async void SocketIO_OnDisconnected(object sender, string e)
        {
            if (Options.Reconnection)
            {
                this.Attempts = 0;
                await ConnectCoreAsync(true);
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
        }
    }
}