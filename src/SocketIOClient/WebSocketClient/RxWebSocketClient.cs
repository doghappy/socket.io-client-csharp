using System;
using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.WebSocketClient
{
    public class RxWebSocketClient : IWebSocketClient
    {
        public RxWebSocketClient()
        {
            _ws = new ClientWebSocket();
            ReceiveChunkSize = 1024 * 8;
            SendChunkSize = 1024 * 8;
            ConnectionTimeout = TimeSpan.FromSeconds(10);
            ReceiveWait = TimeSpan.FromSeconds(1);

            _onListenError = new Subject<Exception>();
            OnListenError = _onListenError.AsObservable();

            _onAborted = new Subject<Unit>();
            OnAborted = _onAborted.AsObservable();
        }

        public int ReceiveChunkSize { get; set; }
        public int SendChunkSize { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
        public TimeSpan ReceiveWait { get; set; }
        public IObservable<Exception> OnListenError { get; }
        public IObservable<Unit> OnAborted { get; }

        readonly ClientWebSocket _ws;
        readonly Subject<Exception> _onListenError;
        readonly Subject<Unit> _onAborted;


        /// <exception cref="WebSocketException"></exception>
        public async Task ConnectAsync(Uri uri)
        {
            var wsConnectionTokenSource = new CancellationTokenSource(ConnectionTimeout);
            await _ws.ConnectAsync(uri, wsConnectionTokenSource.Token);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
        }

        public async Task<bool> TryDisconnectAsync(CancellationToken cancellationToken)
        {
            if (_ws.State != WebSocketState.Open)
            {
                return false;
            }
            try
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="WebSocketException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public async Task SendAsync(byte[] bytes, CancellationToken cancellationToken)
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
                await _ws.SendAsync(new ArraySegment<byte>(subBuffer), WebSocketMessageType.Text, endOfMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<bool> TrySendAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            if (_ws.State != WebSocketState.Open)
            {
                return false;
            }
            try
            {
                await SendAsync(bytes, cancellationToken);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="WebSocketException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        public async Task SendAsync(string text, CancellationToken cancellationToken)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            await SendAsync(bytes, cancellationToken);
        }

        public async Task<bool> TrySendAsync(string text, CancellationToken cancellationToken)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return await TrySendAsync(bytes, cancellationToken);
        }

        public IConnectableObservable<Message> Listen()
        {
            return Observable.Create<Message>(async observer =>
            {
                Console.WriteLine("Create Observable");
                while (true)
                {
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
                                result = await _ws.ReceiveAsync(new ArraySegment<byte>(subBuffer), CancellationToken.None);

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
                                _onListenError.OnNext(e);
                                break;
                            }
                        }

                        if (result == null)
                        {
                            if (_ws.State == WebSocketState.Aborted)
                            {
                                _onAborted.OnNext(Unit.Default);
                            }
                        }
                        else
                        {
                            var message = new Message
                            {
                                Type = result.MessageType
                            };
                            switch (result.MessageType)
                            {
                                case WebSocketMessageType.Text:
                                    message.Text = Encoding.UTF8.GetString(buffer, 0, count);
                                    break;
                                case WebSocketMessageType.Binary:
                                    message.Binary = new byte[count];
                                    Buffer.BlockCopy(buffer, 0, message.Binary, 0, count);
                                    break;
                                case WebSocketMessageType.Close:
                                    break;
                                default:
                                    break;
                            }
                            observer.OnNext(message);
                        }
                    }
                    else
                    {
                        await Task.Delay(ReceiveWait);
                    }
                }
            }).Publish();
        }

        public void Dispose()
        {
            _ws.Dispose();
        }
    }
}
