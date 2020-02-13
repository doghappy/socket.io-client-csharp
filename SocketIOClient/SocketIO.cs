using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SocketIOClient.Arguments;
using SocketIOClient.Parsers;

namespace SocketIOClient
{
    public class SocketIO
    {
        /// <summary>
        /// Options 1: http://example.com/subpath + /socket.io + /sionamespace + ture
        /// Options 2: http://example.com/sionamespace + param path unuse + param sionamespace unuse + ture
        /// </summary>
        /// <param name="uri">新版：HTTP(s)服务地址，结尾不需要斜杠。例如：https://example.com/subpath。旧版：http://example.com/sionamespace</param>
        /// <param name="path">Socket.io服务路径。socket.io在服务端的参数path，默认为/socket.io。例如：/subpath/which/provide/socket/io/service。参考https://socket.io/docs/server-api/</param>
        /// <param name="sionamespace">命名空间，若不使用传入空字符串即可。参考https://socket.io/docs/rooms-and-namespaces/</param>
        /// <param name="usePath">兼容选项，false时兼容旧版本</param>
        public SocketIO(Uri uri, string path, string sionamespace, bool usePath)
        {

            if (uri.Scheme == "https" || uri.Scheme == "http" || uri.Scheme == "wss" || uri.Scheme == "ws")
            {
                // uri = http://example.com:1234/sionamespace
                // path = /path
                // uri.Scheme = http
                // uri.Authority = example.com:1234
                // _uri = http://example.com:1234/path
                if (!usePath)
                    _uri = new Uri(uri.Scheme + "://" + uri.Authority + path);
                else
                    _uri = new Uri(uri.AbsoluteUri + path);
            }
            else
            {
                throw new ArgumentException("Unsupported protocol");
            }
            EventHandlers = new Dictionary<string, EventHandler>();
            Callbacks = new Dictionary<int, EventHandler>();
            _urlConverter = new UrlConverter();

            if (!usePath)
            {
                if (uri.AbsolutePath != "/")
                    _namespace = uri.AbsolutePath + ',';
            }
            else if (sionamespace.Length > 0)
            {
                _namespace = sionamespace + ',';
            }

            _packetId = -1;
            ConnectTimeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// http://example.com/sionamespace
        /// </summary>
        /// <param name="uri">http://example.com/sionamespace</param>
        public SocketIO(string uri) : this(new Uri(uri)) { }
        /// <summary>
        /// http://example.com/sionamespace
        /// </summary>
        /// <param name="uri">http://example.com/sionamespace</param>
        public SocketIO(Uri uri) : this(uri, false) { }
        /// <summary>
        ///  Options 1: http://example.com/sionamespace + false
        ///  Options 2: http://example.com/subpath + true
        /// </summary>
        /// <param name="uri">http://example.com/sionamespace or http://example.com/subpath</param>
        /// <param name="usePath">false or true</param>
        public SocketIO(string uri, bool usePath) : this(new Uri(uri), usePath) { }
        /// <summary>
        ///  Options 1: http://example.com/sionamespace + false
        ///  Options 2: http://example.com/subpath + true
        /// </summary>
        /// <param name="uri">http://example.com/sionamespace or http://example.com/subpath</param>
        /// <param name="usePath">false or true</param>
        public SocketIO(Uri uri, bool usePath) : this(uri, "/socket.io", "", usePath) { }
        /// <summary>
        /// http://example.com/subpath + /sionamespace
        /// </summary>
        /// <param name="uri">http://example.com/subpath</param>
        /// <param name="sionamespace">/sionamespace</param>
        public SocketIO(string uri, string sionamespace) : this(new Uri(uri), sionamespace) { }
        /// <summary>
        /// http://example.com/subpath + /sionamespace
        /// </summary>
        /// <param name="uri">http://example.com/subpath</param>
        /// <param name="sionamespace">/sionamespace</param>
        public SocketIO(Uri uri, string sionamespace) : this(uri, "/socket.io", sionamespace, true) { }
        /// <summary>
        /// http://example.com/subpath + /socket.io + /sionamespace
        /// </summary>
        /// <param name="uri">http://example.com/subpath</param>
        /// <param name="path">/socket.io</param>
        /// <param name="sionamespace">/sionamespace</param>
        public SocketIO(string uri, string path, string sionamespace) : this(new Uri(uri), path, sionamespace, true) { }
        /// <summary>
        /// http://example.com/subpath + /socket.io + /sionamespace
        /// </summary>
        /// <param name="uri">http://example.com/subpath</param>
        /// <param name="path">/socket.io</param>
        /// <param name="sionamespace">/sionamespace</param>
        public SocketIO(Uri uri, string path, string sionamespace) : this(uri, path, sionamespace, true) { }

        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;

        readonly Uri _uri;
        private ClientWebSocket _socket;
        readonly UrlConverter _urlConverter;
        readonly string _namespace;
        private CancellationTokenSource _tokenSource;
        private int _packetId;
        public Dictionary<int, EventHandler> Callbacks { get; }

        public int EIO { get; set; } = 3;
        public TimeSpan ConnectTimeout { get; set; }
        public Dictionary<string, string> Parameters { get; set; }

        public event Action OnConnected;
        public event Action<ResponseArgs> OnError;
        public event Action<ServerCloseReason> OnClosed;
        public event Action<string, ResponseArgs> UnhandledEvent;
        public event Action<string, ResponseArgs> OnReceivedEvent;

        public Dictionary<string, EventHandler> EventHandlers { get; }

        public SocketIOState State { get; private set; }

        public Task ConnectAsync()
        {
            _tokenSource = new CancellationTokenSource();
            Uri wsUri = _urlConverter.HttpToWs(_uri, EIO.ToString(), Parameters);
            if (_socket != null)
            {
                _socket.Dispose();
            }
            _socket = new ClientWebSocket();
            bool executed = _socket.ConnectAsync(wsUri, CancellationToken.None).Wait(ConnectTimeout);
            if (!executed)
            {
                throw new TimeoutException();
            }
            Listen();
            return Task.CompletedTask;
        }

        public Task CloseAsync()
        {
            if (_socket == null)
            {
                throw new InvalidOperationException("Close failed, must connect first.");
            }
            else
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
                _socket.Abort();
                _socket.Dispose();
                _socket = null;
                OnClosed?.Invoke(ServerCloseReason.ClosedByClient);
                return Task.CompletedTask;
            }
        }

        private void Listen()
        {
            // Listen State
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(500);
                    if (_socket.State == WebSocketState.Aborted || _socket.State == WebSocketState.Closed)
                    {
                        if (State != SocketIOState.Closed)
                        {
                            State = SocketIOState.Closed;
                            _tokenSource.Cancel();
                            OnClosed?.Invoke(ServerCloseReason.Aborted);
                        }
                    }
                }
            }, _tokenSource.Token);

            // Listen Message
            Task.Factory.StartNew(async () =>
            {
                var buffer = new byte[ReceiveChunkSize];
                while (true)
                {
                    if (_socket.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _tokenSource.Token);
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var builder = new StringBuilder();
                            string str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            builder.Append(str);

                            while (!result.EndOfMessage)
                            {
                                result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _tokenSource.Token);
                                str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                                builder.Append(str);
                            }

                            var parser = new ResponseTextParser(_namespace, this)
                            {
                                Text = builder.ToString()
                            };
                            await parser.ParseAsync();
                        }
                    }
                }
            }, _tokenSource.Token);
        }

        private async Task SendMessageAsync(string text)
        {
            if (_socket.State == WebSocketState.Open)
            {
                var messageBuffer = Encoding.UTF8.GetBytes(text);
                var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);

                for (var i = 0; i < messagesCount; i++)
                {
                    int offset = SendChunkSize * i;
                    int count = SendChunkSize;
                    bool isEndOfMessage = (i + 1) == messagesCount;

                    if ((count * (i + 1)) > messageBuffer.Length)
                    {
                        count = messageBuffer.Length - offset;
                    }

                    await _socket.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, isEndOfMessage, _tokenSource.Token);
                }
            }
        }

        public Task InvokeConnectedAsync()
        {
            State = SocketIOState.Connected;
            OnConnected?.Invoke();
            return Task.CompletedTask;
        }

        public async Task InvokeClosedAsync()
        {
            if (State != SocketIOState.Closed)
            {
                State = SocketIOState.Closed;
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _tokenSource.Token);
                _tokenSource.Cancel();
                OnClosed?.Invoke(ServerCloseReason.ClosedByServer);
            }
        }

        public async Task InvokeOpenedAsync(OpenedArgs args)
        {
            await Task.Factory.StartNew(async () =>
            {
                if (_namespace != null)
                {
                    await SendMessageAsync("40" + _namespace);
                }
                State = SocketIOState.Connected;
                while (true)
                {
                    if (State == SocketIOState.Connected)
                    {
                        await Task.Delay(args.PingInterval);
                        await SendMessageAsync(((int)EngineIOProtocol.Ping).ToString());
                    }
                    else
                    {
                        break;
                    }
                }
            });
        }

        public Task InvokeUnhandledEvent(string eventName, ResponseArgs args)
        {
            UnhandledEvent?.Invoke(eventName, args);
            return Task.CompletedTask;
        }

        public Task InvokeReceivedEvent(string eventName, ResponseArgs args)
        {
            OnReceivedEvent?.Invoke(eventName, args);
            return Task.CompletedTask;
        }

        public Task InvokeErrorEvent(ResponseArgs args)
        {
            OnError?.Invoke(args);
            return Task.CompletedTask;
        }

        public void On(string eventName, EventHandler handler)
        {
            EventHandlers.Add(eventName, handler);
        }

        private async Task EmitAsync(string eventName, int packetId, object obj)
        {
            string text = JsonConvert.SerializeObject(obj);
            var builder = new StringBuilder();
            builder
                .Append("42")
                .Append(_namespace)
                .Append(packetId)
                .Append('[')
                .Append('"')
                .Append(eventName)
                .Append('"')
                .Append(',')
                .Append(text)
                .Append(']');

            string message = builder.ToString();
            if (State == SocketIOState.Connected)
            {
                await SendMessageAsync(message);
            }
            else
            {
                throw new InvalidOperationException("Socket connection not ready, emit failure.");
            }
        }

        public async Task EmitAsync(string eventName, object obj)
        {
            _packetId++;
            await EmitAsync(eventName, _packetId, obj);
        }

        public async Task EmitAsync(string eventName, object obj, EventHandler callback)
        {
            _packetId++;
            Callbacks.Add(_packetId, callback);
            await EmitAsync(eventName, _packetId, obj);
        }
    }
}
