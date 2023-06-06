using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SocketIO.Serializer.Core;
using SocketIOClient.Extensions;
using SocketIOClient.Messages;
using SocketIO.Core;

namespace SocketIOClient.Transport
{
    public abstract class BaseTransport : ITransport
    {
        protected BaseTransport(TransportOptions options, ISerializer serializer)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Serializer = serializer;
            _messageQueue = new ConcurrentQueue<IMessage2>();
        }

        protected const string DirtyMessage = "Invalid object's current state, may need to create a new object.";

        DateTime _pingTime;
        readonly ConcurrentQueue<IMessage2> _messageQueue;
        protected TransportOptions Options { get; }
        protected ISerializer Serializer { get; }

        public Action<IMessage2> OnReceived { get; set; }

        protected abstract TransportProtocol Protocol { get; }
        protected CancellationTokenSource PingTokenSource { get; private set; }
        protected IMessage2 OpenedMessage2 { get; private set; }

        public string Namespace { get; set; }
        public Action<Exception> OnError { get; set; }

        // public async Task SendAsync(IMessage msg, CancellationToken cancellationToken)
        // {
        //     msg.EIO = Options.EIO;
        //     msg.Protocol = Protocol;
        //     var payload = new Payload
        //     {
        //         Text = msg.Write()
        //     };
        //     if (msg.OutgoingBytes != null)
        //     {
        //         payload.Bytes = msg.OutgoingBytes;
        //     }
        //
        //     await SendAsync(payload, cancellationToken).ConfigureAwait(false);
        // }

        public abstract Task SendAsync(IList<SerializedItem> items, CancellationToken cancellationToken);

        protected virtual async Task OpenAsync(IMessage2 msg)
        {
            OpenedMessage2 = msg;
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
                    // await SendAsync(connectMsg, CancellationToken.None).ConfigureAwait(false);
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
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(OpenedMessage2.PingInterval, cancellationToken);
                    try
                    {
                        var ping = Serializer.NewMessage(MessageType.Ping);
                        using (var cts = new CancellationTokenSource(OpenedMessage2.PingTimeout))
                        {
                            // await SendAsync(ping, cts.Token).ConfigureAwait(false);
                            Debug.WriteLine("Ping");
                        }

                        _pingTime = DateTime.Now;
                        OnReceived.TryInvoke(ping);
                    }
                    catch (Exception e)
                    {
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
            if (PingTokenSource != null && !PingTokenSource.IsCancellationRequested)
            {
                PingTokenSource.Cancel();
                PingTokenSource.Dispose();
            }
        }

        protected async Task OnTextReceived(string text)
        {
            // TODO: refactor by eio
            Debug.WriteLine($"[{Protocol}⬇] {text}");
            var msg = Serializer.Deserialize(Options.EIO, text);
            if (msg == null) return;

            if (msg.BinaryCount > 0)
            {
                msg.IncomingBytes = new List<byte[]>(msg.BinaryCount);
                _messageQueue.Enqueue(msg);
                return;
            }

            if (msg.Type == MessageType.Opened)
            {
                await OpenAsync(msg).ConfigureAwait(false);
            }

            if (Options.EIO == EngineIO.V3)
            {
                if (msg.Type == MessageType.Connected)
                {
                    int ms = 0;
                    while (OpenedMessage2 is null)
                    {
                        await Task.Delay(10);
                        ms += 10;
                        if (ms > Options.ConnectionTimeout.TotalMilliseconds)
                        {
                            OnError.TryInvoke(new TimeoutException());
                            return;
                        }
                    }

                    msg.Sid = OpenedMessage2.Sid;
                    // if ((string.IsNullOrEmpty(Namespace) && string.IsNullOrEmpty(msg.Namespace)) ||
                    //     msg.Namespace == Namespace)
                    if (msg.Namespace == Namespace)
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
                    msg.Duration = DateTime.Now - _pingTime;
                }
            }

            OnReceived.TryInvoke(msg);

            if (msg.Type == MessageType.Ping)
            {
                _pingTime = DateTime.Now;
                try
                {
                    // await SendAsync(new PongMessage(), CancellationToken.None).ConfigureAwait(false);
                    var pong = Serializer.NewMessage(MessageType.Pong);
                    pong.Duration = DateTime.Now - _pingTime;
                    OnReceived.TryInvoke(pong);
                }
                catch (Exception e)
                {
                    OnError.TryInvoke(e);
                }
            }
        }

        protected void OnBinaryReceived(byte[] bytes)
        {
            Debug.WriteLine($"[{Protocol}⬇]0️⃣1️⃣0️⃣1️⃣");
            
            if (_messageQueue.Count <= 0) 
                return;
            if (!_messageQueue.TryPeek(out var msg)) 
                return;
            
            msg.ReceivedBinary.Add(bytes);
            
            if (msg.ReceivedBinary.Count < msg.BinaryCount)
                return;
            
            _messageQueue.TryDequeue(out var result);
            OnReceived.TryInvoke(result);
        }
    }
}