using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Extensions;

namespace SocketIOClient.Transport.Http
{
    public class Eio4HttpPollingHandler : HttpPollingHandler
    {
        public Eio4HttpPollingHandler(IHttpClient adapter) : base(adapter)
        {
        }

        const char Separator = '\u001E';

        public override async Task PostAsync(string uri, IEnumerable<byte[]> bytes, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();
            foreach (var item in bytes)
            {
                builder.Append('b').Append(Convert.ToBase64String(item)).Append(Separator);
            }
            if (builder.Length == 0)
            {
                return;
            }
            string text = builder.ToString().TrimEnd(Separator);
            await PostAsync(uri, text, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task ProduceText(string text)
        {
            string[] items = text.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                if (item[0] == 'b')
                {
                    byte[] bytes = Convert.FromBase64String(item.Substring(1));
                    await OnBytes(bytes).ConfigureAwait(false);
                }
                else
                {
                    await OnTextReceived.TryInvokeAsync(item).ConfigureAwait(false);
                }
            }
        }
    }
}