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
        public HttpTransport(HttpClient httpClient, int eio)
        {
            _client = httpClient;
            _eio = eio;
        }

        readonly HttpClient _client;
        readonly int _eio;

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
            if (_eio == 3)
            {
                while (true)
                {
                    int index = text.IndexOf(':');
                    if (index == -1)
                    {
                        break;
                    }
                    if (int.TryParse(text.Substring(0, index), out int length))
                    {
                        string msg = text.Substring(index + 1, length);
                        OnTextReceived(msg);
                        if (index + length + 1 > text.Length - 1)
                        {
                            break;
                        }
                    }
                    else
                    {// 这里有问题，F5 启动 Sample 测试
                        break;
                    }
                }
            }
            else
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
}
