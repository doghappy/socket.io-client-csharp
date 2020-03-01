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
        public SocketIO(Uri uri)
        {
            if (uri.Scheme == "https" || uri.Scheme == "http" || uri.Scheme == "wss" || uri.Scheme == "ws")
            {
                _uri = uri;
            }
            else
            {
                throw new ArgumentException("Unsupported protocol");
            }
            EventHandlers = new Dictionary<string, EventHandler>();
            Callbacks = new Dictionary<int, EventHandler>();
            _urlConverter = new UrlConverter();
            if (_uri.AbsolutePath != "/")
            {
                _namespace = _uri.AbsolutePath + ',';
            }
            _packetId = -1;
            ConnectTimeout = TimeSpan.FromSeconds(30);
        }

        public SocketIO(string uri) : this(new Uri(uri)) { }

        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;

        readonly Uri _uri;
        private System.Net.WebSockets.Managed.ClientWebSocket _socket;
        readonly UrlConverter _urlConverter;
        readonly string _namespace;
        private CancellationTokenSource _tokenSource;
        private int _packetId;
        public Dictionary<int, EventHandler> Callbacks { get; }

        public int EIO { get; set; } = 3;
        public string Path { get; set; }
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
            Uri wsUri = _urlConverter.HttpToWs(_uri, EIO.ToString(), Path, Parameters);
            if (_socket != null)
            {
                _socket.Dispose();
            }
            _socket = new System.Net.WebSockets.Managed.ClientWebSocket();
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
