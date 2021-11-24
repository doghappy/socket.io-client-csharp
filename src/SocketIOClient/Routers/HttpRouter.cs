using SocketIOClient.Messages;
using SocketIOClient.Transport;
using SocketIOClient.UriConverters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Routers
{
    public class HttpRouter : Router
    {
        public HttpRouter(HttpClient httpClient, Func<IClientWebSocket> clientWebSocketProvider, SocketIOOptions options) : base(httpClient, clientWebSocketProvider, options)
        {
        }

        HttpTransport _httpTransport;
        CancellationTokenSource _pollingTokenSource;
        string _httpUri;

        protected override TransportProtocol Protocol => TransportProtocol.Polling;

        public override async Task ConnectAsync()
        {
            await base.ConnectAsync();

            Uri uri = UriConverter.GetServerUri(false, ServerUri, EIO, Options.Path, Options.Query);
            var req = new HttpRequestMessage(HttpMethod.Get, uri);
            if (Options.ExtraHeaders != null)
            {
                foreach (var item in Options.ExtraHeaders)
                {
                    req.Headers.Add(item.Key, item.Value);
                }
            }

            if (EIO == 3)
            {
                _httpTransport = new HttpEio3Transport(HttpClient);
            }
            else
            {
                _httpTransport = new HttpEio4Transport(HttpClient);
            }

            _httpTransport.OnTextReceived = OnTextReceived;
            _httpTransport.OnBinaryReceived = OnBinaryReceived;

            await _httpTransport.SendAsync(req, new CancellationTokenSource(Options.ConnectionTimeout).Token).ConfigureAwait(false);
            if (_pollingTokenSource != null)
            {
                _pollingTokenSource.Cancel();
            }
            _pollingTokenSource = new CancellationTokenSource();

            StartPolling(_pollingTokenSource.Token);
        }

        private void StartPolling(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await _httpTransport.GetAsync(_httpUri, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch
                    {
                        OnTransportClosed();
                        throw;
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        protected override async Task OpenAsync(OpenedMessage msg)
        {
            Uri uri = UriConverter.GetServerUri(false, ServerUri, EIO, Options.Path, Options.Query);
            _httpUri = uri + "&sid=" + msg.Sid;
            await base.OpenAsync(msg);
        }

        public override async Task DisconnectAsync()
        {
            _pollingTokenSource.Cancel();
            await base.DisconnectAsync();
        }

        protected override async Task SendAsync(string text, CancellationToken cancellationToken)
        {
            if (EIO == 3)
            {
                text = text.Length + ":" + text;
            }
            await _httpTransport.PostAsync(_httpUri, text, cancellationToken).ConfigureAwait(false);
            Debug.WriteLine($"[Send] {text}");
        }

        protected override async Task SendAsync(IEnumerable<byte[]> bytes, CancellationToken cancellationToken)
        {
            await _httpTransport.PostAsync(_httpUri, bytes, cancellationToken).ConfigureAwait(false);
        }
    }
}
