using SocketIOClient.Messages;
using SocketIOClient.Transport;
using SocketIOClient.UriConverters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Routers
{
    public abstract class Router : IDisposable
    {
        public Router(HttpClient httpClient, Func<IClientWebSocket> clientWebSocketProvider, SocketIOOptions options)
        {
            HttpClient = httpClient;
            ClientWebSocketProvider = clientWebSocketProvider;
            UriConverter = new UriConverter();
            _messageQueue = new Queue<IMessage>();
            Options = options;
        }

        protected HttpClient HttpClient { get; }
        readonly Queue<IMessage> _messageQueue;
        protected Func<IClientWebSocket> ClientWebSocketProvider { get; }
        protected SocketIOOptions Options { get; }

        protected OpenedMessage OpenedMessage { get; set; }
        CancellationTokenSource _pingTokenSource;
        DateTime _pingTime;

        public Uri ServerUri { get; set; }

        public string Namespace { get; set; }

        protected abstract TransportProtocol Protocol { get; }

        public int EIO { get; private set; }

        public IUriConverter UriConverter { get; set; }

        public Action<IMessage> OnMessageReceived { get; set; }

        public Action OnTransportClosed { get; set; }

        public virtual Task ConnectAsync()
        {
            EIO = Options.EIO;
            return Task.CompletedTask;
        }

        protected virtual async Task OpenAsync(OpenedMessage msg)
        {
            OpenedMessage = msg;
            var connectMsg = new ConnectedMessage
            {
                Namespace = Namespace,
                Eio = EIO,
                Query = Options.Query
            };

            for (int i = 1; i <= 3; i++)
            {
                try
                {
                    await SendAsync(connectMsg.Write(), CancellationToken.None).ConfigureAwait(false);
                    break;
                }
                catch (Exception e)
                {
                    if (i == 3)
                        Trace.TraceError(e.ToString());
                    else
                        await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, i) * 100));
                }
            }
            /*
            try
            {
                await Policy
                   .Handle<Exception>()
                   .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100))
                   .ExecuteAsync(async () =>
                   {
                       await SendAsync(connectMsg.Write(), CancellationToken.None).ConfigureAwait(false);
                   }).ConfigureAwait(false);

            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                OnTransportClosed();
            }
            */
        }

        /// <summary>
        /// <para>Eio3 ping is sent by the client</para>
        /// <para>Eio4 ping is sent by the server</para>
        /// </summary>
        /// <param name="cancellationToken"></param>
        private void StartPing(CancellationToken cancellationToken)
        {
            Debug.WriteLine($"[Ping] Interval: {OpenedMessage.PingInterval}");
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(OpenedMessage.PingInterval);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    try
                    {
                        var ping = new PingMessage();
                        Debug.WriteLine($"[Ping] Sending");
                        await SendAsync(ping, CancellationToken.None).ConfigureAwait(false);
                        Debug.WriteLine($"[Ping] Has been sent");
                        _pingTime = DateTime.Now;
                        OnMessageReceived(ping);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"[Ping] Failed to send, {e.Message}");
                        OnTransportClosed();
                        throw;
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        protected async void OnTextReceived(string text)
        {
            Debug.WriteLine($"[Receive] {text}");
            var msg = MessageFactory.CreateMessage(EIO, text);
            if (msg != null)
            {
                if (msg.BinaryCount > 0)
                {
                    msg.IncomingBytes = new List<byte[]>(msg.BinaryCount);
                    _messageQueue.Enqueue(msg);
                }
                else
                {
                    if (msg.Type == MessageType.Opened)
                    {
                        await OpenAsync(msg as OpenedMessage).ConfigureAwait(false);
                    }

                    if (EIO == 3)
                    {
                        if (msg.Type == MessageType.Connected)
                        {
                            var connectMsg = msg as ConnectedMessage;
                            connectMsg.Sid = OpenedMessage.Sid;
                            if ((string.IsNullOrEmpty(Namespace) && string.IsNullOrEmpty(connectMsg.Namespace)) || connectMsg.Namespace == Namespace)
                            {
                                if (_pingTokenSource != null)
                                {
                                    _pingTokenSource.Cancel();
                                }
                                _pingTokenSource = new CancellationTokenSource();
                                StartPing(_pingTokenSource.Token);
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

                    OnMessageReceived(msg);
                    if (msg.Type == MessageType.Ping)
                    {
                        _pingTime = DateTime.Now;
                        try
                        {
                            await SendAsync(new PongMessage
                            {
                                Eio = EIO,
                                Protocol = Protocol
                            }, CancellationToken.None).ConfigureAwait(false);
                            OnMessageReceived(new PongMessage
                            {
                                Eio = EIO,
                                Protocol = Protocol,
                                Duration = DateTime.Now - _pingTime
                            });
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                            OnTransportClosed();
                        }
                    }
                }
            }
        }

        protected void OnBinaryReceived(byte[] bytes)
        {
            Debug.WriteLine($"[Receive] binary message");
            if (_messageQueue.Count > 0)
            {
                var msg = _messageQueue.Peek();
                msg.IncomingBytes.Add(bytes);
                if (msg.IncomingBytes.Count == msg.BinaryCount)
                {
                    OnMessageReceived(msg);
                    _messageQueue.Dequeue();
                }
            }
        }

        protected void OnAborted(Exception e)
        {
            Debug.WriteLine($"[Websocket] Aborted, " + e.Message);
            OnTransportClosed();
        }

        public async Task SendAsync(IMessage msg, CancellationToken cancellationToken)
        {
            msg.Eio = EIO;
            msg.Protocol = Protocol;
            string text = msg.Write();
            await SendAsync(text, cancellationToken).ConfigureAwait(false);
            if (msg.OutgoingBytes != null)
            {
                await SendAsync(msg.OutgoingBytes, cancellationToken).ConfigureAwait(false);
            }
        }

        public virtual Task DisconnectAsync()
        {
            if (_pingTokenSource != null)
            {
                _pingTokenSource.Cancel();
            }
            return Task.CompletedTask;
        }

        protected abstract Task SendAsync(string text, CancellationToken cancellationToken);

        protected abstract Task SendAsync(IEnumerable<byte[]> bytes, CancellationToken cancellationToken);

        public virtual void Dispose()
        {
            _messageQueue.Clear();
        }
    }
}
