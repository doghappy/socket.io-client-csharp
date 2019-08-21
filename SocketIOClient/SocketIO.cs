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
            _emitQueue = new Queue<string>();
        }

        public SocketIO(string uri) : this(new Uri(uri)) { }

        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;

        readonly Uri _uri;
        private ClientWebSocket _socket;
        readonly UrlConverter _urlConverter;
        readonly string _namespace;
        private CancellationTokenSource _tokenSource;
        private int _packetId;
        public Dictionary<int, EventHandler> Callbacks { get; }
        public ResponseTextParser Parser { get; private set; }
        readonly Queue<string> _emitQueue;

        public int EIO { get; set; } = 3;
        public Dictionary<string, string> Parameters { get; set; }

        public event Action OnConnected;

        public event Action<ServerCloseReason> OnClosed;
        public event Action<string, ResponseArgs> UnhandledEvent;

        public Dictionary<string, EventHandler> EventHandlers { get; }

        public SocketIOState State { get; private set; }

        public async Task ConnectAsync()
        {
            Parser = new ResponseTextParser(_namespace, this);
            _tokenSource = new CancellationTokenSource();
            Uri wsUri = _urlConverter.HttpToWs(_uri, EIO.ToString(), Parameters);
            if (_socket != null)
            {
                _socket.Dispose();
            }
            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(wsUri, _tokenSource.Token);
            Listen();
        }

        public async Task CloseAsync()
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _tokenSource.Token);
            _socket.Dispose();
            _tokenSource.Cancel();
            OnClosed?.Invoke(ServerCloseReason.ClosedByClient);
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

                            Parser.Text = builder.ToString();
                            await Parser.ParseAsync();
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

        public async Task InvokeConnectedAsync()
        {
            State = SocketIOState.Connected;
            OnConnected?.Invoke();
            while (_emitQueue.Count > 0)
            {
                string item = _emitQueue.Dequeue();
                await SendMessageAsync(item);
            }
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
            else if (State == SocketIOState.None)
            {
                _emitQueue.Enqueue(message);
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
