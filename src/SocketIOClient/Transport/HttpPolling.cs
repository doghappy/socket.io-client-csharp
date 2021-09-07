using System;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport
{
    public class HttpPolling : IObservable<TransportMessage>
    {
        public HttpPolling(HttpClient httpClient)
        {
            _client = httpClient;
            _onReceived = new Subject<TransportMessage>();
        }

        readonly HttpClient _client;

        readonly Subject<TransportMessage> _onReceived;

        public string HandshakeUri { get; set; }
        public string WorkUri { get; set; }

        public async Task GetAsync(string uri, CancellationToken cancellationToken)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, uri);
            var resMsg = await _client.SendAsync(req, cancellationToken).ConfigureAwait(false);
            string text = await resMsg.Content.ReadAsStringAsync().ConfigureAwait(false);
            Produce(text);
        }

        public async Task PostAsync(string uri, string content, CancellationToken cancellationToken)
        {
            var httpContent = new StringContent(content);
            var resMsg = await _client.PostAsync(uri, httpContent, cancellationToken).ConfigureAwait(false);
            string text = await resMsg.Content.ReadAsStringAsync().ConfigureAwait(false);
            Produce(text);
        }

        public async Task PostAsync(string uri, byte[] bytes, CancellationToken cancellationToken)
        {
            var content = new ByteArrayContent(bytes);
            var resMsg = await _client.PostAsync(uri, content, cancellationToken).ConfigureAwait(false);
            string text = await resMsg.Content.ReadAsStringAsync().ConfigureAwait(false);
            Produce(text);
        }

        private void Produce(string text)
        {
            var msg = new TransportMessage();
            if (text[0] == 'b')
            {
                msg.Type = TransportMessageType.Binary;
                msg.Binary = Convert.FromBase64String(text.Substring(1));
            }
            else
            {
                msg.Type = TransportMessageType.Text;
                msg.Text = text;
            }
            _onReceived.OnNext(msg);
        }

        public IDisposable Subscribe(IObserver<TransportMessage> observer)
        {
            return _onReceived.Subscribe(observer);
        }
    }
}
