using System;
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
        public HttpTransport(TransportOptions options) : base(options)
        {
            _httpClientHandler = new HttpClientHandler();
            _httpClient = new HttpClient(_httpClientHandler);
            _pollingHandler = CreateHttpPollingHandler(_httpClient);
            _sendLock = new SemaphoreSlim(1);
        }

        bool _dirty;
        string _httpUri;
        CancellationTokenSource _pollingTokenSource;

        readonly HttpClient _httpClient;
        readonly HttpClientHandler _httpClientHandler;
        readonly IHttpPollingHandler _pollingHandler;
        readonly SemaphoreSlim _sendLock;

        protected override TransportProtocol Protocol => TransportProtocol.Polling;

        private IHttpPollingHandler CreateHttpPollingHandler(HttpClient httpClient)
        {
            switch (Options.EIO)
            {
                case EngineIO.V3:
                    return new Eio3HttpPollingHandler(httpClient);
                default:
                    return new Eio4HttpPollingHandler(httpClient);
            }
        }

        private void StartPolling(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!_httpUri.Contains("&sid="))
                    {
                        await Task.Delay(20);
                        continue;
                    }
                    try
                    {
                        await _pollingHandler.GetAsync(_httpUri, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        OnError.TryInvoke(e);
                        break;
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        public override async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (_dirty) throw new ObjectNotCleanException();
            var req = new HttpRequestMessage(HttpMethod.Get, uri);
            _httpUri = uri.ToString();
            await _pollingHandler.SendAsync(req, new CancellationTokenSource(Options.ConnectionTimeout).Token).ConfigureAwait(false);
            _dirty = true;
            _pollingTokenSource = new CancellationTokenSource();
            StartPolling(_pollingTokenSource.Token);
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
            _httpClient.DefaultRequestHeaders.Add(key, val);
        }

        public override void SetProxy(IWebProxy proxy)
        {
            if (_dirty)
            {
                throw new InvalidOperationException("Unable to set proxy after connecting");
            }
            _httpClientHandler.Proxy = proxy;
        }

        public override void Dispose()
        {
            base.Dispose();
            _pollingTokenSource.TryCancel();
            _pollingTokenSource.TryDispose();
        }

        public override async Task SendAsync(Payload payload, CancellationToken cancellationToken)
        {
            await _pollingHandler.PostAsync(_httpUri, payload.Text, cancellationToken);
            if (payload.Bytes != null && payload.Bytes.Count > 0)
            {
                await _pollingHandler.PostAsync(_httpUri, payload.Bytes, cancellationToken);
            }
        }

        protected override async Task OpenAsync(OpenedMessage msg)
        {
            _httpUri += "&sid=" + msg.Sid;
            await base.OpenAsync(msg);
        }
    }
}
