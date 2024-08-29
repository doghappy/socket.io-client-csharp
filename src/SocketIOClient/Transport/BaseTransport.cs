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

namespace SocketIOClient.Transport
{
    public abstract class BaseTransport(TransportOptions options, ISerializer serializer) : ITransport
    {
        private DateTime _pingTime;
        protected TransportOptions Options { get; } = options;
        private ISerializer Serializer { get; } = serializer;

        public Action<IMessage> OnReceived { get; set; }

        protected abstract TransportProtocol Protocol { get; }
        protected CancellationTokenSource PingTokenSource { get; set; }
        public Action<Exception> OnError { get; set; }

        public abstract Task SendAsync(IList<SerializedItem> items, CancellationToken cancellationToken);

        protected virtual async Task OpenAsync(IMessage message)
        {
            Options.OpenedMessage = message;
            if (Options.EIO == EngineIO.V3 && string.IsNullOrEmpty(Options.Namespace))
            {
                return;
            }

            var connectedMessage = Serializer.SerializeConnectedMessage(
                Options.EIO,
                Options.Namespace,
                Options.Auth,
                Options.Query);

            await SendAsync(new List<SerializedItem>
            {
                connectedMessage
            }, CancellationToken.None).ConfigureAwait(false);
        }

        protected void StartPing(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(Options.OpenedMessage.PingInterval, cancellationToken);
                    try
                    {
                        using (var cts = new CancellationTokenSource(Options.OpenedMessage.PingTimeout))
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

        protected abstract Task ConnectCoreAsync(Uri uri, CancellationToken cancellationToken);

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            var uri = GetConnectionUri();
            try
            {
                await ConnectCoreAsync(uri, cancellationToken);
            }
            catch (Exception e)
            {
                throw new TransportException($"Could not connect to '{uri}'", e);
            }
        }

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
                const int delay = 100;
                while (Options.OpenedMessage is null)
                {
                    await Task.Delay(delay);
                    ms += delay;
                    if (ms <= Options.ConnectionTimeout.TotalMilliseconds) continue;
                    OnError.TryInvoke(new TimeoutException());
                    return true;
                }

                message.Sid = Options.OpenedMessage.Sid;
                if (message.Namespace == Options.Namespace
                    || (DefaultNamespaces.Contains(Options.Namespace) && DefaultNamespaces.Contains(message.Namespace)))
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

        private Uri GetConnectionUri()
        {
            var builder = new StringBuilder();
            builder
                .Append(GetConnectionUriSchema())
                .Append("://")
                .Append(Options.ServerUri.Host);
            if (!Options.ServerUri.IsDefaultPort)
            {
                builder.Append(':').Append(Options.ServerUri.Port);
            }

            builder.Append(string.IsNullOrEmpty(Options.Path) ? "/socket.io" : Options.Path);

            builder
                .Append("/?EIO=")
                .Append((int)Options.EIO)
                .Append("&transport=")
                .Append(Protocol.ToString().ToLower());

            if (Options.OpenedMessage is not null)
            {
                builder.Append("&sid=").Append(Options.OpenedMessage.Sid);
            }

            foreach (var item in Options.Query)
            {
                builder.Append('&').Append(item.Key).Append('=').Append(item.Value);
            }

            return new Uri(builder.ToString());
        }

        private string GetConnectionUriSchema()
        {
            return Options.ServerUri.Scheme switch
            {
                "https" or "wss" => Protocol == TransportProtocol.Polling ? "https" : "wss",
                "http" or "ws" => Protocol == TransportProtocol.Polling ? "http" : "ws",
                _ => throw new ArgumentException("Only supports 'http, https, ws, wss' protocol")
            };
        }
    }
}