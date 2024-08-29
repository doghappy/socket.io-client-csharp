using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketIO.Serializer.Core;
using SocketIOClient.Extensions;
using SocketIO.Core;

namespace SocketIOClient.Transport.WebSockets
{
    public class WebSocketTransport : BaseTransport
    {
        public WebSocketTransport(TransportOptions options, IClientWebSocket ws, ISerializer serializer) 
            : base(options, serializer)
        {
            _ws = ws;
            _sendLock = new SemaphoreSlim(1, 1);
            _listenCancellation = new CancellationTokenSource();
        }

        protected override TransportProtocol Protocol => TransportProtocol.WebSocket;

        readonly IClientWebSocket _ws;
        readonly SemaphoreSlim _sendLock;
        readonly CancellationTokenSource _listenCancellation;
        int _sendChunkSize = ChunkSize.Size8K;
        int _receiveChunkSize = ChunkSize.Size8K;
        // TODO: is it needed?
        bool _dirty;

        private async Task SendAsync(TransportMessageType type, byte[] bytes, CancellationToken cancellationToken)
        {
            if (type == TransportMessageType.Binary && Options.EIO == EngineIO.V3)
            {
                byte[] buffer = new byte[bytes.Length + 1];
                buffer[0] = 4;
                Buffer.BlockCopy(bytes, 0, buffer, 1, bytes.Length);
                bytes = buffer;
            }

            int pages = (int)Math.Ceiling(bytes.Length * 1.0 / _sendChunkSize);
            for (int i = 0; i < pages; i++)
            {
                int offset = i * _sendChunkSize;
                int length = _sendChunkSize;
                if (offset + length > bytes.Length)
                {
                    length = bytes.Length - offset;
                }

                byte[] subBuffer = new byte[length];
                Buffer.BlockCopy(bytes, offset, subBuffer, 0, subBuffer.Length);
                bool endOfMessage = pages - 1 == i;
                await _ws.SendAsync(subBuffer, type, endOfMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        private void Listen(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var binary = new byte[_receiveChunkSize];
                    int count = 0;
                    WebSocketReceiveResult result = null;

                    while (_ws.State == WebSocketState.Open)
                    {
                        try
                        {
                            result = await _ws.ReceiveAsync(_receiveChunkSize, cancellationToken).ConfigureAwait(false);

                            // resize
                            if (binary.Length - count < result.Count)
                            {
                                Array.Resize(ref binary, binary.Length + result.Count);
                            }

                            Buffer.BlockCopy(result.Buffer, 0, binary, count, result.Count);
                            count += result.Count;
                            if (result.EndOfMessage)
                            {
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            OnError.TryInvoke(e);
#if DEBUG
                            Debug.WriteLine($"[{Protocol}❌] {e}");
#endif
                            return;
                        }
                    }

                    if (result == null)
                    {
                        return;
                    }

                    try
                    {
                        switch (result.MessageType)
                        {
                            case TransportMessageType.Text:
                                string text = Encoding.UTF8.GetString(binary, 0, count);
                                await OnTextReceived(text);
                                break;
                            case TransportMessageType.Binary:
                                byte[] bytes;
                                if (Options.EIO == EngineIO.V3)
                                {
                                    bytes = new byte[count - 1];
                                    Buffer.BlockCopy(binary, 1, bytes, 0, bytes.Length);
                                }
                                else
                                {
                                    bytes = new byte[count];
                                    Buffer.BlockCopy(binary, 0, bytes, 0, bytes.Length);
                                }

                                await OnBinaryReceived(bytes);
                                break;
                            case TransportMessageType.Close:
                                OnError.TryInvoke(new TransportException("Received a Close message"));
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        OnError.TryInvoke(e);
#if DEBUG
                        Debug.WriteLine($"[{Protocol}❌] {e}");
#endif
                        break;
                    }
                }
            }, cancellationToken);
        }

        protected override async Task ConnectCoreAsync(Uri uri, CancellationToken cancellationToken)
        {
            // if (_dirty)
            //     throw new InvalidOperationException(DirtyMessage);
            // _dirty = true;
            await _ws.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
            Listen(_listenCancellation.Token);
        }

        public override async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            await _ws.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task SendAsync(IList<SerializedItem> items, CancellationToken cancellationToken)
        {
            try
            {
                await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (items[0].Type == SerializedMessageType.Text)
                {
                    var bytes = Encoding.UTF8.GetBytes(items[0].Text);
                    await SendAsync(TransportMessageType.Text, bytes, cancellationToken);
                    Debug.WriteLine($"[WebSocket⬆] {items[0].Text}");
                }

                var binary = items.AllBinary();
                if (binary.Count > 0)
                {
                    foreach (var b in binary)
                    {
                        await SendAsync(TransportMessageType.Binary, b, cancellationToken).ConfigureAwait(false);
                    }

                    Debug.WriteLine($"[WebSocket⬆]0️⃣1️⃣0️⃣1️⃣ x {binary.Count}");
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void OnUpgraded()
        {
            if (Options.EIO == EngineIO.V3)
            {
                PingTokenSource?.Cancel();
                PingTokenSource = new CancellationTokenSource();
                StartPing(PingTokenSource.Token);   
            }
        }

        public async Task ChangeSendChunkSizeAsync(int size)
        {
            try
            {
                await _sendLock.WaitAsync().ConfigureAwait(false);
                _sendChunkSize = size;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task ChangeReceiveChunkSizeAsync(int size)
        {
            try
            {
                await _sendLock.WaitAsync().ConfigureAwait(false);
                _sendChunkSize = size;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public override void AddHeader(string key, string val)
        {
            if (_dirty)
            {
                throw new InvalidOperationException("Unable to add header after connecting");
            }

            _ws.AddHeader(key, val);
        }

        public override void SetProxy(IWebProxy proxy)
        {
            if (_dirty)
            {
                throw new InvalidOperationException("Unable to set proxy after connecting");
            }

            _ws.SetProxy(proxy);
        }

        public override void Dispose()
        {
            base.Dispose();
            _sendLock.Dispose();
        }
    }
}