using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SocketIO.Serializer.Core;
using SocketIOClient.Extensions;
using SocketIO.Core;

namespace SocketIOClient.Transport
{
    public abstract class BaseTransport : ITransport
    {
        protected BaseTransport(TransportOptions options, ISerializer serializer)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Serializer = serializer;
        }

        protected const string DirtyMessage = "Invalid object's current state, may need to create a new object.";

        DateTime _pingTime;
        protected TransportOptions Options { get; }
        protected ISerializer Serializer { get; }

        public Action<IMessage> OnReceived { get; set; }

        protected abstract TransportProtocol Protocol { get; }
        protected CancellationTokenSource PingTokenSource { get; private set; }
        protected IMessage OpenedMessage { get; private set; }

        public string Namespace { get; set; }
        public Action<Exception> OnError { get; set; }

        public abstract Task SendAsync(IList<SerializedItem> items, CancellationToken cancellationToken);

        protected virtual async Task OpenAsync(IMessage message)
        {
            OpenedMessage = message;
            if (Options.EIO == EngineIO.V3 && string.IsNullOrEmpty(Namespace))
            {
                return;
            }

            var connectedMessage = Serializer.SerializeConnectedMessage(
                Options.EIO,
                Namespace,
                Options.Auth,
                Options.Query);

            await SendAsync(new List<SerializedItem>
            {
                connectedMessage
            }, CancellationToken.None).ConfigureAwait(false);
        }

        private void StartPing(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(OpenedMessage.PingInterval, cancellationToken);
                    try
                    {
                        using (var cts = new CancellationTokenSource(OpenedMessage.PingTimeout))
                        {
                            Debug.WriteLine("Ping");
                            await SendAsync(new List<SerializedItem>
                            {
                                Serializer.SerializePingMessage()
                            }, cts.Token).ConfigureAwait(false);
                        }

                        _pingTime = DateTime.Now;
                        OnReceived.TryInvoke(Serializer.NewMessage(MessageType.Ping));
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

        private static readonly HashSet<string> DefaultNamespaces = new()
        {
            null,
            string.Empty,
            "/"
        };

        private async Task<bool> HandleEio3Messages(IMessage message)
        {
            if (Options.EIO != EngineIO.V3) return false;
            if (message.Type == MessageType.Pong)
            {
                message.Duration = DateTime.Now - _pingTime;
            }
            else if (message.Type == MessageType.Connected)
            {
                var ms = 0;
                while (OpenedMessage is null)
                {
                    await Task.Delay(10);
                    ms += 10;
                    if (ms <= Options.ConnectionTimeout.TotalMilliseconds) continue;
                    OnError.TryInvoke(new TimeoutException());
                    return true;
                }

                message.Sid = OpenedMessage.Sid;
                if (message.Namespace == Namespace
                    || (DefaultNamespaces.Contains(Namespace) && DefaultNamespaces.Contains(message.Namespace)))
                {
                    PingTokenSource?.Cancel();

                    PingTokenSource = new CancellationTokenSource();
                    StartPing(PingTokenSource.Token);
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private async Task HandlePingMessage(IMessage message)
        {
            if (Options.EIO == EngineIO.V3) return;
            if (message.Type == MessageType.Ping)
            {
                _pingTime = DateTime.Now;
                try
                {
                    await SendAsync(new List<SerializedItem>
                    {
                        Serializer.SerializePongMessage()
                    }, CancellationToken.None).ConfigureAwait(false);
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

        protected async Task OnTextReceived(string text)
        {
            Debug.WriteLine($"[{Protocol}⬇] {text}");
            var message = Serializer.Deserialize(Options.EIO, text);
            await HandleMessage(message).ConfigureAwait(false);
        }

        protected async Task OnBinaryReceived(byte[] bytes)
        {
            Debug.WriteLine($"[{Protocol}⬇]0️⃣1️⃣0️⃣1️⃣");
            var message = Serializer.Deserialize(Options.EIO, bytes);
            await HandleMessage(message).ConfigureAwait(false);
        }

        private async Task HandleMessage(IMessage message)
        {
            if (message == null) return;

            if (message.Type == MessageType.Opened)
            {
                await OpenAsync(message).ConfigureAwait(false);
            }

            if (await HandleEio3Messages(message)) return;

            OnReceived.TryInvoke(message);

            await HandlePingMessage(message).ConfigureAwait(false);
        }
    }
}