using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SocketIOClient.Arguments;
using SocketIOClient.Parsers;
using Websocket.Client;

namespace SocketIOClient
{
    public class SocketIO : IDisposable
    {
        public SocketIO(Uri uri)
        {
            _ctx = new ParserContext();
            _builder = new ParserContextBuilder(uri, _ctx);
            ConnectTimeout = TimeSpan.FromSeconds(30);
            _pingSource = new CancellationTokenSource();
        }

        public SocketIO(string uri) : this(new Uri(uri)) { }

        private ParserContext _ctx;
        private readonly ParserContextBuilder _builder;
        private WebsocketClient _client;
        private readonly CancellationTokenSource _pingSource;

        public string Path
        {
            get => _ctx.Path;
            set => _ctx.Path = value;
        }

        public Dictionary<string, string> Parameters
        {
            get => _ctx.Parameters;
            set => _ctx.Parameters = value;
        }

        public TimeSpan ConnectTimeout { get; set; }

        public event Action OnConnected;
        public event Action<ResponseArgs> OnError;
        public event Action<ServerCloseReason> OnClosed;
        public event Action<string, ResponseArgs> UnhandledEvent;
        public event Action<string, ResponseArgs> OnReceivedEvent;

        public SocketIOState State { get; private set; }

        private void BuildHandlers()
        {
            _ctx.ConnectHandler = ConnectHandler;
            _ctx.CloseHandler = CloseHandler;
            _ctx.UncaughtHandler = UncaughtHandler;
            _ctx.ReceiveHandler = ReceiveHandler;
            _ctx.ErrorHandler = ErrorHandler;
            _ctx.OpenHandler = OpenHandler;
        }

        public Task ConnectAsync()
        {
            _ctx = _builder.Build();
            BuildHandlers();
            _client = new WebsocketClient(_ctx.WsUri);
            _client.MessageReceived.Subscribe(Listen);
            _client.DisconnectionHappened.Subscribe(info =>
            {
                if (info.Type != DisconnectionType.ByUser)
                {
                    CloseHandler();
                }
            });

            var token = new CancellationTokenSource(ConnectTimeout).Token;
            token.ThrowIfCancellationRequested();
            try
            {
                _client.Start().Wait(token);
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException)
                    throw new TimeoutException();
                throw e;
            }
            return Task.CompletedTask;
        }

        public Task CloseAsync()
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Close failed, must connect first.");
            }
            else
            {
                _client.Stop(WebSocketCloseStatus.NormalClosure, string.Empty);
                _pingSource.Cancel();
                OnClosed?.Invoke(ServerCloseReason.ClosedByClient);
                return Task.CompletedTask;
            }
        }

        private void Listen(ResponseMessage resMsg)
        {
            if (resMsg.MessageType == WebSocketMessageType.Text)
            {
                var parser = new OpenedParser();
                var connectedParser = new ConnectedParser();
                var errorParser = new ErrorParser();
                var disconnectedParser = new DisconnectedParser();
                var msgEventParser = new MessageEventParser();
                var msgAckParser = new MessageAckParser();
                var msgEventBinaryParser = new MessageEventBinaryParser();
                parser.Next = connectedParser;
                connectedParser.Next = errorParser;
                errorParser.Next = disconnectedParser;
                disconnectedParser.Next = msgEventParser;
                msgEventParser.Next = msgAckParser;
                msgAckParser.Next = msgEventBinaryParser;
                parser.Parse(_ctx, resMsg);
            }
            else if (resMsg.MessageType == WebSocketMessageType.Binary)
            {
                _ctx.ReceivedBuffers.Add(resMsg.Binary);
                if (_ctx.ReceivedBuffers.Count == _ctx.BufferCount)
                {
                    var buffers = _ctx.ReceivedBuffers.ToList();
                    foreach (var item in _ctx.BinaryEvents)
                    {
                        item.ResponseArgs.Buffers = buffers;
                        item.EventHandler?.Invoke(item.ResponseArgs);
                    }
                }
            }
        }

        private void ConnectHandler()
        {
            State = SocketIOState.Connected;
            OnConnected?.Invoke();
        }

        private void CloseHandler()
        {
            if (State != SocketIOState.Closed)
            {
                State = SocketIOState.Closed;
                _client.Stop(WebSocketCloseStatus.NormalClosure, string.Empty);
                _pingSource.Cancel();
                OnClosed?.Invoke(ServerCloseReason.ClosedByServer);
            }
        }

        private void UncaughtHandler(string eventName, ResponseArgs args) => UnhandledEvent?.Invoke(eventName, args);

        private void ReceiveHandler(string eventName, ResponseArgs args) => OnReceivedEvent?.Invoke(eventName, args);

        private void ErrorHandler(ResponseArgs args) => OnError?.Invoke(args);

        private void OpenHandler(OpenedArgs args)
        {
            var task = Task.Factory.StartNew(async () =>
            {
                if (_ctx.Namespace != null)
                {
                    await SendMessageAsync("40" + _ctx.Namespace);
                }
                while (!_pingSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(args.PingInterval);
                    await SendMessageAsync("2");
                }
            }, _pingSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private Task SendMessageAsync(string text)
        {
            if (_client.IsRunning && _client.IsStarted)
            {
                _client.Send(text);
            }
            else
            {
                throw new InvalidOperationException("Unable to send message, socket is disconnected.");
            }
            return Task.CompletedTask;
        }

        public void On(string eventName, EventHandler handler, params EventHandler[] moreHandlers)
        {
            _ctx.EventHandlers.Add(JsonConvert.SerializeObject(eventName), new EventHandlerBox
            {
                EventHandler = handler,
                EventHandlers = moreHandlers
            });
        }

        private async Task EmitAsync(string eventName, int packetId, params object[] objs)
        {
            var paramsBuilder = new StringBuilder();
            if (objs == null || objs.Length == 0)
            {
                paramsBuilder.Append("null");
            }
            else
            {
                for (int i = 0; i < objs.Length; i++)
                {
                    paramsBuilder.Append(JsonConvert.SerializeObject(objs[i]));
                    if (i != objs.Length - 1)
                    {
                        paramsBuilder.Append(",");
                    }
                }
            }
            var builder = new StringBuilder();
            builder
                .Append("42")
                .Append(_ctx.Namespace)
                .Append(packetId)
                .Append('[')
                .Append(JsonConvert.SerializeObject(eventName))
                .Append(',')
                .Append(paramsBuilder.ToString())
                .Append(']');
            string message = builder.ToString();
            await SendMessageAsync(message);
        }

        public async Task EmitAsync(string eventName, params object[] objs)
        {
            await EmitAsync(eventName, ++_ctx.PacketId, objs);
        }

        public async Task EmitAsync(string eventName, object obj, EventHandler callback)
        {
            _ctx.Callbacks.Add(++_ctx.PacketId, callback);
            await EmitAsync(eventName, _ctx.PacketId, obj);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
