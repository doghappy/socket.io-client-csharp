using System;
using System.Text;

namespace SocketIOClient
{
    public class UrlConverter
    {
        public Uri HttpToWs(Uri httpUri, SocketIOOptions options)
        {
            var builder = new StringBuilder();
            if (httpUri.Scheme == "https" || httpUri.Scheme == "wss")
            {
                builder.Append("wss://");
            }
            else
            {
                builder.Append("ws://");
            }
            builder.Append(httpUri.Host);
            if (!httpUri.IsDefaultPort)
            {
                builder.Append(":").Append(httpUri.Port);
            }
            builder
                .Append(options.Path)
                .Append("/?EIO=")
                .Append(options.EIO)
                .Append("&transport=websocket");

            if (options.Query != null)
            {
                foreach (var item in options.Query)
                {
                    builder
                        .Append("&")
                        .Append(item.Key)
                        .Append("=")
                        .Append(item.Value);
                }
            }
            return new Uri(builder.ToString());
        }
    }
}
