using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport
{
    public class HttpEio4Transport : HttpTransport
    {
        public HttpEio4Transport(HttpClient httpClient) : base(httpClient) { }

        public override async Task PostAsync(string uri, IEnumerable<byte[]> bytes, CancellationToken cancellationToken)
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

        protected override void ProduceText(string text)
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
