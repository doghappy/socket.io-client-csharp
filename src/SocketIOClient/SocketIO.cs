using SocketIOClient.ConnectInterval;
using SocketIOClient.EioHandler;
using SocketIOClient.EventArguments;
using SocketIOClient.Exceptions;
using SocketIOClient.JsonSerializer;
using SocketIOClient.Response;
using SocketIOClient.WebSocketClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient
{
    /// <summary>
    /// socket.io client class
    /// </summary>
    public class SocketIO
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
                        Namespace = value.AbsolutePath + ',';
                    }
                }
            }
        }

        public UrlConverter UrlConverter { get; set; }

        public IWebSocketClient Socket { get; set; }

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

        [Obsolete("OnReceivedEvent will be removed, please use OnAny() instead")]
        public event EventHandler<ReceivedEventArgs> OnReceivedEvent;

        #endregion

        internal Queue<BelowNormalEvent> BelowNormalEvents { get; private set; }

        private void Initialize()
        {
            UrlConverter = new UrlConverter();
            Socket = new ClientWebSocket(this);
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
            await ConnectCoreAsync(Options.AllowedRetryFirstConnection, null);
        }

        private async Task ConnectCoreAsync(bool allowedRetryFirstConnection, Action connectBefore)
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
                        connectBefore?.Invoke();
                        await Socket.ConnectAsync(wsUri);
                        break;
                    }
                    catch (SystemException ex)
                    {
                        if (ex is TimeoutException || ex is System.Net.WebSockets.WebSocketException)
                        {
                            await Task.Delay(connectInterval.GetDelay());
                            if (connectInterval.NextDealy() > Options.ReconnectionDelayMax)
                            {
                                OnReconnectFailed?.Invoke(this, ex);
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
            else
            {
                await Socket.ConnectAsync(wsUri);
            }
        }

        public async Task DisconnectAsync()
        {
            if (Connected && !Disconnected)
            {
                if (Options.EioHandler is Eio3Handler)
                {
                    var v3 = Options.EioHandler as Eio3Handler;
                    v3.StopPingInterval();
                }
                try
                {
                    await Socket.SendMessageAsync("41" + Namespace);
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
            builder.Append(Namespace);
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
            catch (System.Net.WebSockets.WebSocketException e)
            {
                this.InvokeDisconnect(e.Message);
            }
            catch (InvalidSocketStateException e)
            {
                this.InvokeDisconnect(e.Message);
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
            builder
                .Append(Namespace)
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

        internal void Open(OpenResponse openResponse)
        {
            Id = openResponse.Sid;
            Options.EioHandler.IOConnectAsync(this).Wait();
            if (Options.EioHandler is Eio3Handler)
            {
                var v3 = Options.EioHandler as Eio3Handler;
                v3.PingInterval = openResponse.PingInterval;
                _ = v3.StartPingAsync(this);
            }
        }

        internal void InvokeConnect()
        {
            Connected = true;
            Disconnected = false;
            OnConnected?.Invoke(this, new EventArgs());
        }

        internal void InvokeBytesReceived(byte[] bytes)
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
                        InvokeReceivedEvent(new ReceivedEventArgs
                        {
                            Event = e.Event,
                            Response = e.Response
                        });
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

        internal void InvokeDisconnect(string reason)
        {
            if (Connected)
            {
                Connected = false;
                Disconnected = true;
                if (Options.EioHandler is Eio3Handler)
                {
                    var v3 = Options.EioHandler as Eio3Handler;
                    v3.StopPingInterval();
                }
                OnDisconnected?.Invoke(this, reason);
            }
        }

        internal void InvokeError(string error)
        {
            OnError?.Invoke(this, error);
        }

        private async void SocketIO_OnDisconnected(object sender, string e)
        {
            if (Options.Reconnection)
            {
                int attempt = 0;
                await ConnectCoreAsync(true, () => OnReconnecting?.Invoke(this, ++attempt));
            }
        }

        internal void InvokePong(TimeSpan ms)
        {
            OnPong?.Invoke(this, ms);
        }

        internal void InvokePing()
        {
            OnPing?.Invoke(this, new EventArgs());
        }

        internal void InvokePingV3()
        {
            OnPing?.Invoke(this, new EventArgs());
        }

        [Obsolete]
        internal void InvokeReceivedEvent(ReceivedEventArgs args)
        {
            OnReceivedEvent?.Invoke(this, args);
        }
    }
}