using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.JsonSerializer;
using SocketIOClient.Messages;
using SocketIOClient.Processors;
using SocketIOClient.Transport;
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
                    value.ConnectionTimeout = Options.ConnectionTimeout;
                }
            }
        }

        public TransportRouter Router { get; private set; }

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
        public bool Disconnected => !Connected;

        public SocketIOOptions Options { get; }

        public IJsonSerializer JsonSerializer { get; set; }

        public HttpClient HttpClient { get; set; }

        public IClientWebSocket ClientWebSocket { get; set; }

        int _packetId;
        Queue<BinaryEvent> _binaryEvents;
        Dictionary<int, Action<SocketIOResponse>> _ackHandlers;
        List<OnAnyHandler> _onAnyHandlers;
        Dictionary<string, Action<SocketIOResponse>> _eventHandlers;
        CancellationTokenSource _pingTokenSorce;
        CancellationTokenSource _connectionTokenSorce;
        DateTime _pingTime;
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
            MessageProcessor = new EngineIOProtocolProcessor();
            _packetId = -1;
            _ackHandlers = new Dictionary<int, Action<SocketIOResponse>>();
            _eventHandlers = new Dictionary<string, Action<SocketIOResponse>>();
            _onAnyHandlers = new List<OnAnyHandler>();
            _binaryEvents = new Queue<BinaryEvent>();

            JsonSerializer = new SystemTextJsonSerializer();
            _connectionTokenSorce = new CancellationTokenSource();
            HttpClient = new HttpClient();
            ClientWebSocket = new DefaultClientWebSocket();
        }

        internal static bool IsNamespaceDefault(string @namespace)
        {
            return string.IsNullOrEmpty(@namespace) || @namespace.Equals("/");
        }

        private void CreateRouterIfNull()
        {
            if (Router == null)
            {
                Router = new TransportRouter(HttpClient, ClientWebSocket)
                {
                    AutoUpgrade = Options.AutoUpgrade,
                    Namespace = Namespace,
                    Path = Options.Path,
                    ServerUri = ServerUri,
                    JsonSerializer = JsonSerializer
                };
                if (Options.Query != null)
                {
                    var kvs = new List<KeyValuePair<string, string>>();
                    foreach (var item in Options.Query)
                    {
                        kvs.Add(new KeyValuePair<string, string>(item.Key, item.Value));
                    }
                    Router.QueryParams = kvs;
                }
                Router.OnMessageReceived = OnMessageReceived;
            }
        }

        public async Task ConnectAsync()
        {
            CreateRouterIfNull();
            _reconnectionDelay = Options.ReconnectionDelay;
            while (true)
            {
                try
                {
                    if (_connectionTokenSorce.IsCancellationRequested)
                    {
                        break;
                    }
                    if (Attempts > 0)
                    {
                        OnReconnectAttempt?.Invoke(this, Attempts);
                    }
                    await Router.ConnectAsync().ConfigureAwait(false);
                    break;
                }
                catch (SystemException e)
                {
                    if (e is TimeoutException || e is WebSocketException)
                    {
                        if (!Options.Reconnection)
                        {
                            throw;
                        }
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
                            await Task.Delay((int)_reconnectionDelay).ConfigureAwait(false);
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

        private void SubscribeBinary(TransportMessage m)
        {
            if (m.Type == TransportMessageType.Binary)
            {
                if (_binaryEvents.Count > 0)
                {
                    var element = _binaryEvents.Peek();
                    element.Response.InComingBytes.Add(m.Binary);
                    if (element.Response.InComingBytes.Count == element.Count)
                    {
                        if (element.PacketId == -1)
                        {
                            foreach (var item in _onAnyHandlers)
                            {
                                try
                                {
                                    item(element.Event, element.Response);
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine(e);
                                }
                            }
                            try
                            {
                                _eventHandlers[element.Event](element.Response);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e);
                            }
                        }
                        else
                        {
                            try
                            {
                                _ackHandlers[element.PacketId](element.Response);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e);
                            }
                            _ackHandlers.Remove(element.PacketId);
                        }
                        _binaryEvents.Dequeue();
                    }
                }
            }
        }

        private async void OpenedHandler(OpenedMessage m)
        {
            Id = m.Sid;
            //if (Options.EIO == 3)
            //{
            //    var builder = new StringBuilder();
            //    if (Options.Query != null)
            //    {
            //        foreach (var item in Options.Query)
            //        {
            //            builder.Append(item.Key).Append('=').Append(item.Value);
            //        }
            //    }
            //    var msg = new Eio3ConnectedMessage
            //    {
            //        Namespace = Namespace,
            //        QueryString = builder.ToString()
            //    };
            //    await Router.SendAsync(msg.Write(), CancellationToken.None).ConfigureAwait(false);
            //    StartPing();
            //}
            //else
            //{
            //    //var msg = new Eio4ConnectedMessage
            //    //{
            //    //    Namespace = Namespace
            //    //};
            //    //await Router.SendAsync(msg.Write(), CancellationToken.None).ConfigureAwait(false);
            //}
        }

        private async void PingHandler()
        {
            try
            {
                _pingTime = DateTime.Now;
                await Router.SendAsync(new PongMessage(), CancellationToken.None).ConfigureAwait(false);
                OnPong?.Invoke(this, DateTime.Now - _pingTime);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                InvokeDisconnect(DisconnectReason.PingTimeout);
            }
        }

        private void PongHandler()
        {
            OnPong?.Invoke(this, DateTime.Now - _pingTime);
        }

        private void ConnectedHandler(ConnectedMessage msg)
        {
            //if (Options.EIO == 3)
            //{
            //    if (!string.IsNullOrEmpty(Namespace))
            //    {
            //        var eio3 = m as Eio3ConnectedMessage;
            //        if (eio3.Namespace != Namespace)
            //        {
            //            return;
            //        }
            //    }
            //}
            //else
            //{
            //    //var eio4 = m as Eio4ConnectedMessage;
            //    //
            //    //Router.Sid = Id;
            //}
            Id = msg.Sid;
            Connected = true;
            OnConnected?.Invoke(this, EventArgs.Empty);
            if (Attempts > 0)
            {
                OnReconnected?.Invoke(this, Attempts);
            }
        }

        private void DisconnectedHandler()
        {
            InvokeDisconnect(DisconnectReason.IOServerDisconnect);
        }

        private void EventMessageHandler(EventMessage m)
        {
            var res = new SocketIOResponse(m.JsonElements, this)
            {
                PacketId = m.Id
            };
            foreach (var item in _onAnyHandlers)
            {
                try
                {
                    item(m.Event, res);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
            if (_eventHandlers.ContainsKey(m.Event))
            {
                try
                {
                    _eventHandlers[m.Event](res);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        private void AckMessageHandler(ClientAckMessage m)
        {
            if (_ackHandlers.ContainsKey(m.Id))
            {
                var res = new SocketIOResponse(m.JsonElements, this);
                try
                {
                    _ackHandlers[m.Id](res);
                }
                finally
                {
                    _ackHandlers.Remove(m.Id);
                }
            }
        }

        private void ErrorMessageHandler(ErrorMessage msg)
        {
            //if (Options.EIO == 3)
            //{
            //    var eio3 = m as Eio3ErrorMessage;
            //    OnError?.Invoke(this, eio3.Message);
            //}
            //else
            //{
            //    var eio4 = m as Eio4ErrorMessage;
            //    OnError?.Invoke(this, eio4.Message);
            //}
            OnError?.Invoke(this, msg.Message);
        }

        private void BinaryMessageHandler(BinaryMessage msg)
        {
            if (_eventHandlers.ContainsKey(msg.Event))
            {
                try
                {
                    var response = new SocketIOResponse(msg.JsonElements, this)
                    {
                        PacketId = msg.Id
                    };
                    response.InComingBytes.AddRange(msg.IncomingBytes);
                    _eventHandlers[msg.Event](response);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        private void BinaryAckMessageHandler(ClientBinaryAckMessage msg)
        {
            if (_ackHandlers.ContainsKey(msg.Id))
            {
                try
                {
                    var response = new SocketIOResponse(msg.JsonElements, this)
                    {
                        PacketId = msg.Id,
                    };
                    response.InComingBytes.AddRange(msg.IncomingBytes);
                    _ackHandlers[msg.Id](response);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
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
                        PongHandler();
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
                Debug.WriteLine(e);
            }
        }

        public async Task DisconnectAsync()
        {
            if (Connected)
            {
                //var message = IsNamespaceDefault(Namespace) ? "41" : $"41{Namespace},";
                //await Socket.TrySendAsync(message, CancellationToken.None).ConfigureAwait(false);
                //await Socket.TryDisconnectAsync(CancellationToken.None).ConfigureAwait(false);
                var msg = new DisconnectedMessage
                {
                    Namespace = Namespace
                };
                try
                {
                    await Router.SendAsync(msg.Write(), CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                InvokeDisconnect(DisconnectReason.IOClientDisconnect);
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
                        Namespace = Namespace,
                        Json = result.Json
                    };
                    msg.OutgoingBytes = new List<byte[]>(result.Bytes);
                }
                else
                {
                    msg = new ServerAckMessage
                    {
                        Namespace = Namespace,
                        Id = packetId,
                        Json = result.Json
                    };
                }
            }
            else
            {
                msg = new ServerAckMessage
                {
                    Namespace = Namespace,
                    Id = packetId
                };
            }
            await Router.SendAsync(msg, cancellationToken);
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
            if (data != null && data.Length > 0)
            {
                var result = JsonSerializer.Serialize(data);
                if (result.Bytes.Count > 0)
                {
                    var msg = new BinaryMessage
                    {
                        Namespace = Namespace,
                        OutgoingBytes = new List<byte[]>(result.Bytes),
                        Event = eventName,
                        Json = result.Json
                    };
                    await Router.SendAsync(msg, cancellationToken);
                }
                else
                {
                    var msg = new EventMessage
                    {
                        Namespace = Namespace,
                        Event = eventName,
                        Json = result.Json
                    };
                    await Router.SendAsync(msg, cancellationToken);
                }
            }
            else
            {
                var msg = new EventMessage
                {
                    Namespace = Namespace,
                    Event = eventName
                };
                await Router.SendAsync(msg, cancellationToken);
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
            _ackHandlers.Add(++_packetId, ack);
            if (data != null && data.Length > 0)
            {
                var result = JsonSerializer.Serialize(data);
                if (result.Bytes.Count > 0)
                {
                    var msg = new ClientBinaryAckMessage
                    {
                        Event = eventName,
                        Namespace = Namespace,
                        Json = result.Json,
                        Id = _packetId,
                        OutgoingBytes = new List<byte[]>(result.Bytes)
                    };
                    await Router.SendAsync(msg, cancellationToken);
                }
                else
                {
                    var msg = new ClientAckMessage
                    {
                        Event = eventName,
                        Namespace = Namespace,
                        Id = _packetId,
                        Json = result.Json
                    };
                    await Router.SendAsync(msg.Write(), cancellationToken);
                }
            }
            else
            {
                var msg = new ClientAckMessage
                {
                    Event = eventName,
                    Namespace = Namespace,
                    Id = _packetId
                };
                await Router.SendAsync(msg.Write(), cancellationToken);
            }
        }

        private void InvokeDisconnect(string reason)
        {
            if (Connected)
            {
                Connected = false;
                //if (Options.EIO == 3)
                //{
                //    _pingTokenSorce.Cancel();
                //}
                OnDisconnected?.Invoke(this, reason);
                if (reason != DisconnectReason.IOServerDisconnect && reason != DisconnectReason.IOServerDisconnect)
                {
                    //In the this cases (explicit disconnection), the client will not try to reconnect and you need to manually call socket.connect().
                    if (Options.Reconnection)
                    {
                        _ = ConnectAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        public void Dispose()
        {
            HttpClient.Dispose();
            ClientWebSocket.Dispose();
            Socket.Dispose();
            Router.Dispose();
            _ackHandlers.Clear();
            _onAnyHandlers.Clear();
            _eventHandlers.Clear();
            _binaryEvents.Clear();
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