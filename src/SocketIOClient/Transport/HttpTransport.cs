using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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
            if (!resMsg.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Response status code does not indicate success: {resMsg.StatusCode}");
            }
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

        public async Task PostAsync(string uri, IEnumerable<byte[]> bytes, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();
            foreach (var item in bytes)
            {
                //1E 
                builder.Append('b').Append(Convert.ToBase64String(item)).Append('');
            }
            if (builder.Length == 0)
            {
                return;
            }
            string text = builder.ToString().TrimEnd('');
            await PostAsync(uri, text, cancellationToken);
        }

        private void Produce(string text)
        {
            string[] items = text.Split(new[] { '' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                if (item[0] == 'b')
                {
                    byte[] bytes = Convert.FromBase64String(item.Substring(1));
                    OnBinaryReceived(bytes);
                }
                else
                {
                    OnTextReceived(item);
                }
            }
        }
    }
}
