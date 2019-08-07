using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient.Arguments;
using SocketIOClient.Exceptions;
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
            _openedParser = new OpenedParser();
            _eventHandlers = new Dictionary<string, EventHandler>();
            _urlConverter = new UrlConverter();
            if (_uri.AbsolutePath != "/")
            {
                _namespace = _uri.AbsolutePath + ',';
            }
            _parserRegex = new Regex("^42" + _namespace + @"\[""([\s\w-]+)"",([\s\S]*)\]$");
        }

        public SocketIO(string uri) : this(new Uri(uri)) { }

        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;

        readonly Uri _uri;
        private ClientWebSocket _socket;
        readonly OpenedParser _openedParser;
        readonly UrlConverter _urlConverter;
        readonly string _namespace;
        readonly Regex _parserRegex;
        private CancellationTokenSource _tokenSource;

        public int EIO { get; set; } = 3;
        public Dictionary<string, string> Parameters { get; set; }

        public event Action OnConnected;

        public event Action<ServerCloseReason> OnClosed;

        private readonly Dictionary<string, EventHandler> _eventHandlers;

        public SocketIOState State { get; private set; }

        public async Task ConnectAsync()
        {
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

                            string text = builder.ToString();
                            // Console.WriteLine("Receive: " + text);
                            await PretreatmentAsync(text);
                        }
                    }
                }
            }, _tokenSource.Token);
        }

        private async Task PretreatmentAsync(string text)
        {
            if (_openedParser.Check(text))
            {
                JObject jobj = _openedParser.Parse(text);
                var args = jobj.ToObject<OpenedArgs>();
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
            else if (text == "40" + _namespace)
            {
                State = SocketIOState.Connected;
                OnConnected?.Invoke();
            }
            else if (text == "41" + _namespace)
            {
                if (State != SocketIOState.Closed)
                {
                    State = SocketIOState.Closed;
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _tokenSource.Token);
                    _tokenSource.Cancel();
                    OnClosed?.Invoke(ServerCloseReason.ClosedByServer);
                }
            }
            else if (_parserRegex.IsMatch(text))
            {
                var groups = _parserRegex.Match(text).Groups;
                string eventName = groups[1].Value;
                if (_eventHandlers.ContainsKey(eventName))
                {
                    var handler = _eventHandlers[eventName];
                    handler(new ResponseArgs
                    {
                        Text = groups[2].Value,
                        RawText = text
                    });
                }
            }
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
                    //Console.WriteLine("Send: " + text);
                }
            }
        }

        public void On(string eventName, EventHandler handler)
        {
            _eventHandlers.Add(eventName, handler);
        }

        public async Task EmitAsync(string eventName, object obj)
        {
            string text = JsonConvert.SerializeObject(obj);
            var builder = new StringBuilder();
            builder
                .Append("42")
                .Append(_namespace)
                .Append('[')
                .Append('"')
                .Append(eventName)
                .Append('"')
                .Append(',')
                .Append(text)
                .Append(']');
            //await SendMessageAsync(builder.ToString());
            int i = 0;
            while (true)
            {
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    break;
                }
                else if (State == SocketIOState.Connected)
                {
                    await SendMessageAsync(builder.ToString());
                    break;
                }
                else if (State == SocketIOState.None)
                {
                    i++;
                    await Task.Delay(20);
                    if (i == 1000)
                    {
                        throw new SocketIOEmitFailedException();
                    }
                }
            }
        }
    }
}
