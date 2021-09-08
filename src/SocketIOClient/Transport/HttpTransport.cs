using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport
{
    public class HttpTransport : IReceivable
    {
        public HttpTransport(HttpClient httpClient)
        {
            _client = httpClient;
        }

        readonly HttpClient _client;

        public Action<string> OnTextReceived { get; set; }
        public Action<byte[]> OnBinaryReceived { get; set; }

        string AppendRandom(string uri)
        {
            return uri + "&t=" + DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        public async Task GetAsync(string uri, CancellationToken cancellationToken)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, AppendRandom(uri));
            var resMsg = await _client.SendAsync(req, cancellationToken).ConfigureAwait(false);
            string text = await resMsg.Content.ReadAsStringAsync().ConfigureAwait(false);
            Produce(text);
        }

        public async Task PostAsync(string uri, string content, CancellationToken cancellationToken)
        {
            var httpContent = new StringContent(content);
            var resMsg = await _client.PostAsync(AppendRandom(uri), httpContent, cancellationToken).ConfigureAwait(false);
            string text = await resMsg.Content.ReadAsStringAsync().ConfigureAwait(false);
            Produce(text);
        }

        public async Task PostAsync(string uri, byte[] bytes, CancellationToken cancellationToken)
        {
            var content = new ByteArrayContent(bytes);
            var resMsg = await _client.PostAsync(AppendRandom(uri), content, cancellationToken).ConfigureAwait(false);
            string text = await resMsg.Content.ReadAsStringAsync().ConfigureAwait(false);
            Produce(text);
        }

        private void Produce(string text)
        {
            var msg = new TransportMessage();
            if (text[0] == 'b')
            {
                byte[] bytes = Convert.FromBase64String(text.Substring(1));
                OnBinaryReceived(bytes);
            }
            else
            {
                OnTextReceived(text);
            }
        }
    }
}
