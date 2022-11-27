using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Extensions;
using SocketIOClient.Messages;

namespace SocketIOClient.Transport.Http
{
    public class HttpTransport : BaseTransport
    {
        public HttpTransport(TransportOptions options, IHttpPollingHandler pollingHandler) : base(options)
        {
            _pollingHandler = pollingHandler ?? throw new ArgumentNullException(nameof(pollingHandler));
            _pollingHandler.OnTextReceived = OnTextReceived;
            _pollingHandler.OnBytesReceived = OnBinaryReceived;
            _sendLock = new SemaphoreSlim(1, 1);
        }

        bool _dirty;
        string _httpUri;
        readonly SemaphoreSlim _sendLock;
        CancellationTokenSource _pollingTokenSource;

        private readonly IHttpPollingHandler _pollingHandler;

        protected override TransportProtocol Protocol => TransportProtocol.Polling;

        private void StartPolling(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // if (!_httpUri.Contains("&sid="))
                    // {
                    //     await Task.Delay(20, cancellationToken);
                    //     continue;
                    // }
                    try
                    {
                        await _pollingHandler.GetAsync(_httpUri, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                        break;
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public override async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (_dirty)
                throw new InvalidOperationException(DirtyMessage);
            var req = new HttpRequestMessage(HttpMethod.Get, uri);
            _httpUri = uri.ToString();

            try
            {
                await _pollingHandler.SendAsync(req, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new TransportException($"Could not connect to '{uri}'", e);
            }

            _dirty = true;
        }

        public override Task DisconnectAsync(CancellationToken cancellationToken)
        {
            _pollingTokenSource.Cancel();
            if (PingTokenSource != null)
            {
                PingTokenSource.Cancel();
            }
            return Task.CompletedTask;
        }

        public override void AddHeader(string key, string val)
        {
            _pollingHandler.AddHeader(key, val);
        }

        public override void SetProxy(IWebProxy proxy)
        {
            if (_dirty)
            {
                throw new InvalidOperationException("Unable to set proxy after connecting");
            }
            _pollingHandler.SetProxy(proxy);
        }

        public override void Dispose()
        {
            base.Dispose();
            _pollingTokenSource.TryCancel();
            _pollingTokenSource.TryDispose();
        }

        public override async Task SendAsync(Payload payload, CancellationToken cancellationToken)
        {
            try
            {
                await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(payload.Text))
                {
                    await _pollingHandler.PostAsync(_httpUri, payload.Text, cancellationToken);
#if DEBUG
                    Debug.WriteLine($"[Polling⬆] {payload.Text}");
#endif
                }
                if (payload.Bytes != null && payload.Bytes.Count > 0)
                {
                    await _pollingHandler.PostAsync(_httpUri, payload.Bytes, cancellationToken);
#if DEBUG
                    Debug.WriteLine("[Polling⬆]0️⃣1️⃣0️⃣1️⃣");
#endif
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        protected override async Task OpenAsync(OpenedMessage msg)
        {
            _httpUri += "&sid=" + msg.Sid;
            _pollingTokenSource = new CancellationTokenSource();
            StartPolling(_pollingTokenSource.Token);
            await base.OpenAsync(msg);
        }
    }
}