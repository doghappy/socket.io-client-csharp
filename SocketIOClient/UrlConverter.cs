using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIOClient
{
    public class UrlConverter
    {
        public Uri HttpToWs(Uri httpUri, string eio, string path, Dictionary<string, string> parameters)
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
                .Append(string.IsNullOrWhiteSpace(path) ? "/socket.io" : path)
                .Append("/?EIO=")
                .Append(eio)
                .Append("&transport=websocket");

            if (parameters != null)
            {
                foreach (var item in parameters)
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
