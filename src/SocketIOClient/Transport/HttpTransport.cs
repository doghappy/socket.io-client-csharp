using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http.Headers;

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
            await ProduceMessageAsync(resMsg).ConfigureAwait(false);
        }

        public async Task SendAsync(HttpRequestMessage req, CancellationToken cancellationToken)
        {
            var resMsg = await _client.SendAsync(req, cancellationToken).ConfigureAwait(false);
            if (!resMsg.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Response status code does not indicate success: {resMsg.StatusCode}");
            }
            await ProduceMessageAsync(resMsg).ConfigureAwait(false);
        }

        public async Task PostAsync(string uri, string content, CancellationToken cancellationToken)
        {
            var httpContent = new StringContent(content);
            var resMsg = await _client.PostAsync(AppendRandom(uri), httpContent, cancellationToken).ConfigureAwait(false);
            await ProduceMessageAsync(resMsg).ConfigureAwait(false);
        }

        public async Task PostAsync(string uri, IEnumerable<byte[]> bytes, CancellationToken cancellationToken)
        {
            if (_eio == 3)
            {
                var list = new List<byte>();
                foreach (var item in bytes)
                {
                    list.Add(1);
                    var length = SplitInt(item.Length + 1).Select(x => (byte)x);
                    list.AddRange(length);
                    list.Add(byte.MaxValue);
                    list.Add(4);
                    list.AddRange(item);
                }
                var content = new ByteArrayContent(list.ToArray());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                await _client.PostAsync(AppendRandom(uri), content, cancellationToken).ConfigureAwait(false);
                //var resMsg = await _client.PostAsync(AppendRandom(uri), content, cancellationToken).ConfigureAwait(false);
                //await ProduceMessageAsync(resMsg).ConfigureAwait(false);
            }
            else
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
        }

        private List<int> SplitInt(int number)
        {
            List<int> list = new List<int>();
            while (number > 0)
            {
                list.Add(number % 10);
                number /= 10;
            }
            list.Reverse();
            return list;
        }

        private async Task ProduceMessageAsync(HttpResponseMessage resMsg)
        {
            if (resMsg.Content.Headers.ContentType.MediaType == "application/octet-stream")
            {
                byte[] bytes = await resMsg.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                ProduceBytes(bytes);
            }
            else
            {
                string text = await resMsg.Content.ReadAsStringAsync().ConfigureAwait(false);
                ProduceText(text);
            }
        }

        private void ProduceText(string text)
        {
            if (_eio == 3)
            {
                int p = 0;
                while (true)
                {
                    int index = text.IndexOf(':', p);
                    if (index == -1)
                    {
                        break;
                    }
                    if (int.TryParse(text.Substring(p, index - p), out int length))
                    {
                        string msg = text.Substring(index + 1, length);
                        OnTextReceived(msg);
                    }
                    else
                    {
                        break;
                    }
                    p = index + length + 1;
                    if (p >= text.Length)
                    {
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

        private void ProduceBytes(byte[] bytes)
        {
            int i = 0;
            while (bytes.Length > i + 4)
            {
                byte type = bytes[i];
                var builder = new StringBuilder();
                i++;
                while (bytes[i] != byte.MaxValue)
                {
                    builder.Append(bytes[i]);
                    i++;
                }
                i++;
                int length = int.Parse(builder.ToString());
                if (type == 0)
                {
                    var buffer = new byte[length];
                    Buffer.BlockCopy(bytes, i, buffer, 0, buffer.Length);
                    OnTextReceived(Encoding.UTF8.GetString(buffer));
                }
                else if (type == 1)
                {
                    var buffer = new byte[length - 1];
                    Buffer.BlockCopy(bytes, i + 1, buffer, 0, buffer.Length);
                    OnBinaryReceived(buffer);
                }
                i += length;
            }
        }
    }
}
