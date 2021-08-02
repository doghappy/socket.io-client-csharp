using System;
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
        public ClientWebSocket()
        {
            ReceiveChunkSize = 1024 * 16;
            ConnectionTimeout = TimeSpan.FromSeconds(10);
        }

        public int ReceiveChunkSize { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }

        System.Net.WebSockets.ClientWebSocket _ws;
        readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
        CancellationTokenSource _listenToken;

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
            DisposeWebSocketIfNotNull();
            _ws = new System.Net.WebSockets.ClientWebSocket();

            Config?.Invoke(_ws.Options);
            var wsConnectionTokenSource = new CancellationTokenSource(ConnectionTimeout);
            try
            {
                await _ws.ConnectAsync(uri, wsConnectionTokenSource.Token);
                DisposeListenTokenIfNotNull();
                _listenToken = new CancellationTokenSource();
                _ = ListenAsync(_listenToken.Token);
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
            await SendMessageAsync(text, CancellationToken.None);
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
                await sendLock.WaitAsync();
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
#if DEBUG
                System.Diagnostics.Trace.WriteLine($"⬆ {DateTime.Now} {text}");
#endif
            }
            catch (TaskCanceledException)
            {
#if DEBUG
                System.Diagnostics.Trace.WriteLine($"❌ {DateTime.Now} Cancel \"{text}\"");
#endif
            }
            finally
            {
                sendLock.Release();
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
            await SendMessageAsync(bytes, CancellationToken.None);
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
                await sendLock.WaitAsync();
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, cancellationToken);
#if DEBUG
                System.Diagnostics.Trace.WriteLine($"⬆ {DateTime.Now} Binary message");
#endif
            }
            catch (TaskCanceledException)
            {
#if DEBUG
                System.Diagnostics.Trace.WriteLine($"❌ {DateTime.Now} Cancel Send Binary");
#endif
            }
            finally
            {
                sendLock.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "io client disconnect", CancellationToken.None);
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                var buffer = new byte[ReceiveChunkSize];
                int count = 0;
                WebSocketReceiveResult result = null;
                while (_ws.State == WebSocketState.Open)
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                        var subBuffer = new byte[ReceiveChunkSize];
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(subBuffer), cancellationToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            OnClosed(result.CloseStatusDescription ?? string.Empty);
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
                        OnClosed(e.Message);
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
                    System.Diagnostics.Trace.WriteLine($"⬇ {DateTime.Now} {message}");
#endif
                    if (OnTextReceived is null)
                    {
                        throw new ArgumentNullException(nameof(OnTextReceived));
                    }
                    OnTextReceived(message);
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
#if DEBUG
                    System.Diagnostics.Trace.WriteLine($"⬇ {DateTime.Now} Binary message");
#endif
                    if (OnTextReceived is null)
                    {
                        throw new ArgumentNullException(nameof(OnTextReceived));
                    }
                    byte[] bytes = new byte[count];
                    Buffer.BlockCopy(buffer, 0, bytes, 0, count);
                    OnBinaryReceived(bytes);
                }
            }
        }

        public Action<string> OnTextReceived { get; set; }
        public Action<byte[]> OnBinaryReceived { get; set; }
        public Action<string> OnClosed { get; set; }

        private void DisposeWebSocketIfNotNull()
        {
            if (_ws != null)
                _ws.Dispose();
        }

        private void DisposeListenTokenIfNotNull()
        {
            if (_listenToken != null)
            {
                _listenToken.Cancel();
                _listenToken.Dispose();
            }
        }

        public void Dispose()
        {
            DisposeWebSocketIfNotNull();
            DisposeListenTokenIfNotNull();
            sendLock.Dispose();
        }
    }
}
