using SocketIOClient.Packgers;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using SocketIOClient.Exceptions;

namespace SocketIOClient.WebSocketClient
{
    /// <summary>
    /// Internally uses 'System.Net.WebSockets.ClientWebSocket' as websocket client
    /// </summary>
    public sealed class ClientWebSocket : IWebSocketClient
    {
        public ClientWebSocket(SocketIO io, PackgeManager parser)
        {
            _parser = parser;
            _io = io;
        }

        const int ReceiveChunkSize = 1024;
        //const int SendChunkSize = 1024;

        readonly PackgeManager _parser;
        readonly SocketIO _io;
        System.Net.WebSockets.ClientWebSocket _ws;
        CancellationTokenSource _wsWorkTokenSource;

        public Action<ClientWebSocketOptions> Config { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="options"></param>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="WebSocketException"></exception>
        /// <returns></returns>
        public async Task ConnectAsync(Uri uri)
        {
            if (_ws != null)
                _ws.Dispose();
            _ws = new System.Net.WebSockets.ClientWebSocket();
            Config?.Invoke(_ws.Options);

            _wsWorkTokenSource = new CancellationTokenSource();
            var wsConnectionTokenSource = new CancellationTokenSource(_io.Options.ConnectionTimeout);
            try
            {
                await _ws.ConnectAsync(uri, wsConnectionTokenSource.Token);
                _ = Task.Run(ListenAsync);
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="InvalidSocketStateException"></exception>
        public async Task SendMessageAsync(string text)
        {
            await SendMessageAsync(text, _wsWorkTokenSource.Token);
        }

        public async Task SendMessageAsync(string text, CancellationToken cancellationToken)
        {
            if (_ws == null)
            {
                throw new InvalidSocketStateException("Faild to emit, websocket is not connected yet.");
            }
            if (_ws.State != WebSocketState.Open)
            {
                throw new InvalidSocketStateException("Connection is not open.");
            }

            byte[] bytes = Encoding.UTF8.GetBytes(text);
            try
            {
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
#if DEBUG
                Trace.WriteLine($"⬆ {DateTime.Now} {text}");
#endif
            }
            catch (TaskCanceledException)
            {
#if DEBUG
                Trace.WriteLine($"❌ {DateTime.Now} Cancel \"{text}\"");
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="InvalidSocketStateException"></exception>
        public async Task SendMessageAsync(byte[] bytes)
        {
            await SendMessageAsync(bytes, _wsWorkTokenSource.Token);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="InvalidSocketStateException"></exception>
        public async Task SendMessageAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            if (_ws == null)
            {
                throw new InvalidSocketStateException("Faild to emit, websocket is not connected yet.");
            }
            if (_ws.State != WebSocketState.Open)
            {
                throw new InvalidSocketStateException("Connection is not open.");
            }
            try
            {
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, cancellationToken);
#if DEBUG
                Trace.WriteLine($"⬆ {DateTime.Now} Binary message");
#endif
            }
            catch (TaskCanceledException)
            {
#if DEBUG
                Trace.WriteLine($"❌ {DateTime.Now} Cancel Send Binary");
#endif
            }
        }

        public async Task DisconnectAsync()
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            Close(null);
        }

        private async Task ListenAsync()
        {
            while (true)
            {
                var buffer = new byte[ReceiveChunkSize];
                int count = 0;
                WebSocketReceiveResult result = null;
                while (_ws.State == WebSocketState.Open)
                {
                    try
                    {
                        //result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _wsWorkTokenSource.Token);
                        var subBuffer = new byte[ReceiveChunkSize];
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(subBuffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Close("io server disconnect");
                            break;
                        }
                        else if (result.MessageType == WebSocketMessageType.Text || result.MessageType == WebSocketMessageType.Binary)
                        {
                            if (buffer.Length - count < result.Count)
                            {
                                Array.Resize(ref buffer, buffer.Length + result.Count);
                            }
                            Buffer.BlockCopy(subBuffer, 0, buffer, count, result.Count);
                            count += result.Count;
                        }
                        if (result.EndOfMessage)
                        {
                            break;
                        }
                    }
                    catch (WebSocketException e)
                    {
                        Close(e.Message);
                        break;
                    }
                }
                if (result == null)
                {
                    break;
                }
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, count);
#if DEBUG
                    Trace.WriteLine($"⬇ {DateTime.Now} {message}");
#endif
                    _parser.Unpack(message);
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
#if DEBUG
                    Trace.WriteLine($"⬇ {DateTime.Now} Binary message");
#endif
                    byte[] bytes;
                    if (_io.Options.EIO == 3)
                    {
                        count -= 1;
                        bytes = new byte[count];
                        Buffer.BlockCopy(buffer, 1, bytes, 0, count);
                    }
                    else
                    {
                        bytes = new byte[count];
                        Buffer.BlockCopy(buffer, 0, bytes, 0, count);
                    }
                    _io.InvokeBytesReceived(bytes);
                }
            }
        }

        private void Close(string reason)
        {
            if (reason != null)
            {
                _io.InvokeDisconnect(reason);
            }
        }
    }
}
