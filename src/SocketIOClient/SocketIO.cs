using Newtonsoft.Json;
using SocketIOClient.EventArguments;
using SocketIOClient.JsonConverters;
using SocketIOClient.Packgers;
using SocketIOClient.Response;
using SocketIOClient.WebSocketClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient
{
    public class SocketIO
    {
        public SocketIO(string uri) : this(new Uri(uri)) { }

        public SocketIO(Uri uri) : this(uri, new SocketIOOptions()) { }
        public SocketIO(string uri, SocketIOOptions options) : this(new Uri(uri), options) { }

        public SocketIO(Uri uri, SocketIOOptions options)
        {
            ServerUri = uri;
            UrlConverter = new UrlConverter();
            _options = options;
            Socket = new ClientWebSocket(this, new PackgeManager(this));
            if (uri.AbsolutePath != "/")
            {
                Namespace = uri.AbsolutePath + ',';
            }
            PacketId = -1;
            Acks = new Dictionary<int, Action<SocketIOResponse>>();
            Handlers = new Dictionary<string, Action<SocketIOResponse>>();
            OutGoingBytes = new List<byte[]>();
            _byteArrayConverter = new ByteArrayConverter
            {
                Client = this
            };
            Disconnected = true;
            OnDisconnected += SocketIO_OnDisconnected;
        }

        public Uri ServerUri { get; set; }
        public UrlConverter UrlConverter { get; set; }
        public IWebSocketClient Socket { get; set; }
        public string Id { get; set; }
        public string Namespace { get; }
        public bool Connected { get; private set; }
        public bool Disconnected { get; private set; }
        internal int PacketId { get; private set; }
        internal List<byte[]> OutGoingBytes { get; }
        internal Dictionary<int, Action<SocketIOResponse>> Acks { get; }
        internal DateTime PingTime { get; set; }

        readonly SocketIOOptions _options;
        readonly ByteArrayConverter _byteArrayConverter;

        internal Dictionary<string, Action<SocketIOResponse>> Handlers { get; set; }

        CancellationTokenSource _pingToken;

        #region Socket.IO event
        public event EventHandler OnConnected;
        //public event EventHandler<string> OnConnectError;
        //public event EventHandler<string> OnConnectTimeout;
        //public event EventHandler<string> OnError;
        public event EventHandler<string> OnDisconnected;
        //public event EventHandler<string> OnReconnected;
        //public event EventHandler<string> OnReconnectAttempt;
        //public event EventHandler<string> OnReconnecting;
        //public event EventHandler<string> OnReconnectError;
        //public event EventHandler<string> OnReconnectFailed;
        public event EventHandler OnPing;
        public event EventHandler<TimeSpan> OnPong;
        public event EventHandler<ReceivedEventArgs> OnReceivedEvent;
        internal event EventHandler<byte[]> OnBytesReceived;
        #endregion

        public async Task ConnectAsync()
        {
            Uri wsUri = UrlConverter.HttpToWs(ServerUri, _options);
            await Socket.ConnectAsync(wsUri, new WebSocketConnectionOptions
            {
                ConnectionTimeout = _options.ConnectionTimeout
            });
        }

        public async Task DisconnectAsync()
        {
            await Socket.SendMessageAsync("41" + Namespace);
            Connected = false;
            Disconnected = true;
            await Socket.DisconnectAsync();
            _pingToken.Cancel();
        }

        public void On(string eventName, Action<SocketIOResponse> callback)
        {
            Handlers.Add(eventName, callback);
        }

        private async Task EmityCoreAsync(string eventName, int packetId, string data)
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
            var builder = new StringBuilder();
            if (data != null && data.Length > 0)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    builder.Append(JsonConvert.SerializeObject(data[i], _byteArrayConverter));
                    if (i != data.Length - 1)
                    {
                        builder.Append(",");
                    }
                }
            }
            return builder.ToString();
        }

        public async Task EmitAsync(string eventName, params object[] data)
        {
            string dataString = GetDataString(data);
            await EmityCoreAsync(eventName, -1, dataString);
        }

        public async Task EmitAsync(string eventName, Action<SocketIOResponse> ack, params object[] data)
        {
            Acks.Add(++PacketId, ack);
            string dataString = GetDataString(data);
            await EmityCoreAsync(eventName, PacketId, dataString);
        }

        private async Task SendNamespaceAsync()
        {
            if (!string.IsNullOrEmpty(Namespace))
            {
                var builder = new StringBuilder();
                builder.Append("40");

                if (!string.IsNullOrEmpty(Namespace))
                {
                    builder.Append(Namespace.TrimEnd(','));
                }
                if (_options.Query != null && _options.Query.Count > 0)
                {
                    builder.Append('?');
                    int index = -1;
                    foreach (var item in _options.Query)
                    {
                        index++;
                        builder
                            .Append(item.Key)
                            .Append('=')
                            .Append(item.Value);
                        if (index < _options.Query.Count - 1)
                        {
                            builder.Append('&');
                        }
                    }
                }
                if (!string.IsNullOrEmpty(Namespace))
                {
                    builder.Append(',');
                }
                await Socket.SendMessageAsync(builder.ToString());
            }
        }

        internal void Open(OpenResponse openResponse)
        {
            Id = openResponse.Sid;
            _pingToken = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
            {
                await SendNamespaceAsync();
                while (true)
                {
                    await Task.Delay(openResponse.PingInterval);
                    if (_pingToken.IsCancellationRequested)
                        return;
                    try
                    {
                        PingTime = DateTime.Now;
                        await Socket.SendMessageAsync("2");
                        OnPing?.Invoke(this, new EventArgs());
                    }
                    catch { }
                }
            }, _pingToken.Token);
        }

        internal void InvokeConnect()
        {
            Connected = true;
            Disconnected = false;
            OnConnected?.Invoke(this, new EventArgs());
        }

        internal void InvokeBytesReceived(byte[] bytes)
        {
            OnBytesReceived?.Invoke(this, bytes);
        }

        internal void InvokeDisconnect(string reason)
        {
            if (Connected)
            {
                Connected = false;
                Disconnected = true;
                OnDisconnected?.Invoke(this, reason);
                _pingToken.Cancel();
            }
        }

        private async void SocketIO_OnDisconnected(object sender, string e)
        {
            if (_options.Reconnection)
            {
                double delayDouble = _options.ReconnectionDelay;
                while (true)
                {
                    int delay = (int)delayDouble;
                    Trace.WriteLine($"{DateTime.Now} Reconnection wait {delay} ms");
                    await Task.Delay(delay);
                    Trace.WriteLine($"{DateTime.Now} Delay done");
                    try
                    {
                        if (!Connected && Disconnected)
                        {
                            await ConnectAsync();
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex is TaskCanceledException || ex is System.Net.WebSockets.WebSocketException)
                        {
                            delayDouble += 2 * _options.RandomizationFactor;
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
            }
        }

        internal void InvokePong(TimeSpan ms)
        {
            OnPong?.Invoke(this, ms);
        }

        internal void InvokeReceivedEvent(ReceivedEventArgs args)
        {
            OnReceivedEvent?.Invoke(this, args);
        }
    }
}
