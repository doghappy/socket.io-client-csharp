using System;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport
{
    public class WebSocket : IWebSocket
    {
        public WebSocket()
        {
            _ws = new ClientWebSocket();
            ReceiveChunkSize = 1024 * 8;
            SendChunkSize = 1024 * 8;
            ConnectionTimeout = TimeSpan.FromSeconds(10);
            ReceiveWait = TimeSpan.FromSeconds(1);
            _listenCancellation = new CancellationTokenSource();
            _onError = new Subject<Exception>();
            _onReceived = new Subject<TransportMessage>();
        }

        public int ReceiveChunkSize { get; set; }
        public int SendChunkSize { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
        public TimeSpan ReceiveWait { get; set; }

        readonly ClientWebSocket _ws;
        readonly Subject<Exception> _onError;
        readonly Subject<TransportMessage> _onReceived;
        readonly CancellationTokenSource _listenCancellation;


        /// <exception cref="WebSocketException"></exception>
        public async Task ConnectAsync(Uri uri)
        {
            var wsConnectionTokenSource = new CancellationTokenSource(ConnectionTimeout);
            await _ws.ConnectAsync(uri, wsConnectionTokenSource.Token).ConfigureAwait(false);
            _ = Task.Factory.StartNew(ListenAsync, TaskCreationOptions.LongRunning);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken).ConfigureAwait(false);
        }

        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="WebSocketException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public async Task SendAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            await SendAsync(WebSocketMessageType.Binary, bytes, cancellationToken);
        }

        private async Task SendAsync(WebSocketMessageType type, byte[] bytes, CancellationToken cancellationToken)
        {
            int pages = (int)Math.Ceiling(bytes.Length * 1.0 / SendChunkSize);
            for (int i = 0; i < pages; i++)
            {
                int offset = i * SendChunkSize;
                int length = SendChunkSize;
                if (offset + length > bytes.Length)
                {
                    length = bytes.Length - offset;
                }
                byte[] subBuffer = new byte[length];
                Buffer.BlockCopy(bytes, offset, subBuffer, 0, subBuffer.Length);
                bool endOfMessage = pages - 1 == i;
                await _ws.SendAsync(new ArraySegment<byte>(subBuffer), type, endOfMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="WebSocketException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public async Task SendAsync(string text, CancellationToken cancellationToken)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            await SendAsync(WebSocketMessageType.Text, bytes, cancellationToken);
        }

        private async Task ListenAsync()
        {
            while (true)
            {
                if (_listenCancellation.IsCancellationRequested)
                {
                    break;
                }
                if (_ws.State == WebSocketState.Open)
                {
                    var buffer = new byte[ReceiveChunkSize];
                    int count = 0;
                    WebSocketReceiveResult result = null;

                    while (true)
                    {
                        var subBuffer = new byte[ReceiveChunkSize];
                        try
                        {
                            result = await _ws.ReceiveAsync(new ArraySegment<byte>(subBuffer), CancellationToken.None).ConfigureAwait(false);

                            // resize
                            if (buffer.Length - count < result.Count)
                            {
                                Array.Resize(ref buffer, buffer.Length + result.Count);
                            }
                            Buffer.BlockCopy(subBuffer, 0, buffer, count, result.Count);
                            count += result.Count;
                            if (result.EndOfMessage)
                            {
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            _onError.OnNext(e);
                            break;
                        }
                    }

                    if (result != null)
                    {
                        var message = new TransportMessage();
                        switch (result.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                message.Type = TransportMessageType.Text;
                                message.Text = Encoding.UTF8.GetString(buffer, 0, count);
                                break;
                            case WebSocketMessageType.Binary:
                                message.Type = TransportMessageType.Binary;
                                message.Binary = new byte[count];
                                Buffer.BlockCopy(buffer, 0, message.Binary, 0, count);
                                break;
                            case WebSocketMessageType.Close:
                                message.Type = TransportMessageType.Close;
                                break;
                            default:
                                break;
                        }
                        _onReceived.OnNext(message);
                    }
                }
                else
                {
                    await Task.Delay(ReceiveWait);
                }
            }
        }

        public IDisposable Subscribe(IObserver<TransportMessage> observer)
        {
            //return _onReceived.Subscribe(x =>
            //{
            //    observer.OnNext(x);
            //});
            return _onReceived.Subscribe(observer);
        }

        public IDisposable Subscribe(IObserver<Exception> observer)
        {
            return _onError.Subscribe(observer);
        }

        public void Dispose()
        {
            _listenCancellation.Cancel();
            _ws.Dispose();
        }
    }
}
