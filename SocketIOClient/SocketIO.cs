using System;
using System.Collections.Generic;
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
            if (uri.Scheme == "https" || uri.Scheme == "http" || uri.Scheme == "wss" || uri.Scheme == "ws")
            {
                _uri = uri;
            }
            else
            {
                throw new ArgumentException("Unsupported protocol");
            }
            EventHandlers = new Dictionary<string, EventHandlerBox>();
            Callbacks = new Dictionary<int, EventHandler>();
            _urlConverter = new UrlConverter();
            if (_uri.AbsolutePath != "/")
            {
                _namespace = _uri.AbsolutePath + ',';
            }
            _packetId = -1;
            ConnectTimeout = TimeSpan.FromSeconds(30);
            _pingSource = new CancellationTokenSource();
            _bufferHandlerQueue = new Queue<EventHandler>();
        }

        public SocketIO(string uri) : this(new Uri(uri)) { }

        readonly Uri _uri;
        private WebsocketClient _client;
        readonly UrlConverter _urlConverter;
        readonly string _namespace;
        private CancellationTokenSource _pingSource;
        private int _packetId;
        private Queue<EventHandler> _bufferHandlerQueue;
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

        public Dictionary<string, EventHandlerBox> EventHandlers { get; }

        public SocketIOState State { get; private set; }

        public Task ConnectAsync()
        {
            Uri wsUri = _urlConverter.HttpToWs(_uri, EIO.ToString(), Path, Parameters);
            _client = new WebsocketClient(wsUri);
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

        private void Listen(ResponseMessage message)
        {
            if (message.MessageType == WebSocketMessageType.Text)
            {
                _bufferHandlerQueue.Clear();
                Console.WriteLine($"Message received: {message.Text}");
                var parser = new ResponseTextParser(_namespace, this)
                {
                    Text = message.Text,
                    BufferHandlerQueue = _bufferHandlerQueue,
                    ConnectHandler = ConnectHandler,
                    CloseHandler = CloseHandler,
                    UncaughtHandler = UncaughtHandler,
                    ReceiveHandler = ReceiveHandler,
                    ErrorHandler = ErrorHandler,
                    OpenHandler = OpenHandler
                };
                parser.Parse();
            }
            else if (message.MessageType == WebSocketMessageType.Binary)
            {
                Console.WriteLine("Buffer received: " + Encoding.UTF8.GetString(message.Binary));
                if (_bufferHandlerQueue.Count > 0)
                {
                    var handler = _bufferHandlerQueue.Dequeue();
                    handler(new ResponseArgs
                    {
                        Buffer = message.Binary
                    });
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
                if (_namespace != null)
                {
                    await SendMessageAsync("40" + _namespace);
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
                //Console.WriteLine($"Message sent: {text}");
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
            EventHandlers.Add(JsonConvert.SerializeObject(eventName), new EventHandlerBox
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
                .Append(_namespace)
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
            _packetId++;
            await EmitAsync(eventName, _packetId, objs);
        }

        public async Task EmitAsync(string eventName, object obj, EventHandler callback)
        {
            _packetId++;
            Callbacks.Add(_packetId, callback);
            await EmitAsync(eventName, _packetId, obj);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
