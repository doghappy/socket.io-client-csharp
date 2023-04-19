using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Extensions;
using SocketIOClient.Messages;

#if DEBUG
using System.Diagnostics;
#endif

namespace SocketIOClient.Transport
{
    public abstract class BaseTransport : ITransport
    {
        protected BaseTransport(TransportOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _messageQueue = new Queue<IMessage>();
        }

        protected const string DirtyMessage = "Invalid object's current state, may need to create a new object.";

        DateTime _pingTime;
        readonly Queue<IMessage> _messageQueue;
        protected TransportOptions Options { get; }

        public Action<IMessage> OnReceived { get; set; }

        protected abstract TransportProtocol Protocol { get; }
        protected CancellationTokenSource PingTokenSource { get; private set; }
        protected OpenedMessage OpenedMessage { get; private set; }

        public string Namespace { get; set; }
        public Action<Exception> OnError { get; set; }

        public async Task SendAsync(IMessage msg, CancellationToken cancellationToken)
        {
            msg.EIO = Options.EIO;
            msg.Protocol = Protocol;
            var payload = new Payload
            {
                Text = msg.Write()
            };
            if (msg.OutgoingBytes != null)
            {
                payload.Bytes = msg.OutgoingBytes;
            }

            await SendAsync(payload, cancellationToken).ConfigureAwait(false);
        }

        protected virtual async Task OpenAsync(OpenedMessage msg)
        {
            OpenedMessage = msg;
            if (Options.EIO == EngineIO.V3 && string.IsNullOrEmpty(Namespace))
            {
                return;
            }

            var connectMsg = new ConnectedMessage
            {
                Namespace = Namespace,
                EIO = Options.EIO,
                Query = Options.Query,
            };
            if (Options.EIO == EngineIO.V4)
            {
                connectMsg.AuthJsonStr = Options.Auth;
            }

            for (int i = 1; i <= 3; i++)
            {
                try
                {
                    await SendAsync(connectMsg, CancellationToken.None).ConfigureAwait(false);
                    break;
                }
                catch (Exception e)
                {
                    if (i == 3)
                        OnError.TryInvoke(e);
                    else
                        await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, i) * 100));
                }
            }
        }

        private void StartPing(CancellationToken cancellationToken)
        {
            // _logger.LogDebug($"[Ping] Interval: {OpenedMessage.PingInterval}");
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(OpenedMessage.PingInterval, cancellationToken);
                    try
                    {
                        var ping = new PingMessage();
                        // _logger.LogDebug($"[Ping] Sending");
                        using (var cts = new CancellationTokenSource(OpenedMessage.PingTimeout))
                        {
                            await SendAsync(ping, cts.Token).ConfigureAwait(false);
                        }

                        // _logger.LogDebug($"[Ping] Has been sent");
                        _pingTime = DateTime.Now;
                        OnReceived.TryInvoke(ping);
                    }
                    catch (Exception e)
                    {
                        // _logger.LogDebug($"[Ping] Failed to send, {e.Message}");
                        OnError.TryInvoke(e);
                        break;
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        public abstract Task ConnectAsync(Uri uri, CancellationToken cancellationToken);

        public abstract Task DisconnectAsync(CancellationToken cancellationToken);

        public abstract void AddHeader(string key, string val);
        public abstract void SetProxy(IWebProxy proxy);

        public virtual void Dispose()
        {
            _messageQueue.Clear();
            if (PingTokenSource != null && !PingTokenSource.IsCancellationRequested)
            {
                PingTokenSource.Cancel();
                PingTokenSource.Dispose();
            }
        }

        public abstract Task SendAsync(Payload payload, CancellationToken cancellationToken);

        protected async Task OnTextReceived(string text)
        {
            // TODO: refactor
#if DEBUG
            Debug.WriteLine($"[{Protocol}⬇] {text}");
#endif
            var msg = MessageFactory.CreateMessage(Options.EIO, text);
            if (msg == null)
            {
                return;
            }

            msg.Protocol = Protocol;
            if (msg.BinaryCount > 0)
            {
                msg.IncomingBytes = new List<byte[]>(msg.BinaryCount);
                _messageQueue.Enqueue(msg);
                return;
            }

            if (msg.Type == MessageType.Opened)
            {
                await OpenAsync(msg as OpenedMessage).ConfigureAwait(false);
            }

            if (Options.EIO == EngineIO.V3)
            {
                if (msg.Type == MessageType.Connected)
                {
                    int ms = 0;
                    while (OpenedMessage is null)
                    {
                        await Task.Delay(10);
                        ms += 10;
                        if (ms > Options.ConnectionTimeout.TotalMilliseconds)
                        {
                            OnError.TryInvoke(new TimeoutException());
                            return;
                        }
                    }

                    var connectMsg = msg as ConnectedMessage;
                    connectMsg.Sid = OpenedMessage.Sid;
                    if ((string.IsNullOrEmpty(Namespace) && string.IsNullOrEmpty(connectMsg.Namespace)) ||
                        connectMsg.Namespace == Namespace)
                    {
                        if (PingTokenSource != null)
                        {
                            PingTokenSource.Cancel();
                        }

                        PingTokenSource = new CancellationTokenSource();
                        StartPing(PingTokenSource.Token);
                    }
                    else
                    {
                        return;
                    }
                }
                else if (msg.Type == MessageType.Pong)
                {
                    var pong = msg as PongMessage;
                    pong.Duration = DateTime.Now - _pingTime;
                }
            }

            OnReceived.TryInvoke(msg);

            if (msg.Type == MessageType.Ping)
            {
                _pingTime = DateTime.Now;
                try
                {
                    await SendAsync(new PongMessage(), CancellationToken.None).ConfigureAwait(false);
                    OnReceived.TryInvoke(new PongMessage
                    {
                        EIO = Options.EIO,
                        Protocol = Protocol,
                        Duration = DateTime.Now - _pingTime
                    });
                }
                catch (Exception e)
                {
                    OnError.TryInvoke(e);
                }
            }
        }

        protected void OnBinaryReceived(byte[] bytes)
        {
#if DEBUG
            Debug.WriteLine($"[{Protocol}⬇]0️⃣1️⃣0️⃣1️⃣");
#endif
            if (_messageQueue.Count > 0)
            {
                var msg = _messageQueue.Peek();
                msg.IncomingBytes.Add(bytes);
                if (msg.IncomingBytes.Count == msg.BinaryCount)
                {
                    OnReceived.TryInvoke(msg);
                    _messageQueue.Dequeue();
                }
            }
        }
    }
}