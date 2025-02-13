using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIOClient.V2.UriConverter
{
    public class DefaultUriConverter(int eio) : IUriConverter
    {
        public Uri GetServerUri(bool ws, Uri serverUri, string path, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var builder = new StringBuilder();
            SetSchema(ws, serverUri, builder);
            builder.Append(serverUri.Host);
            if (!serverUri.IsDefaultPort)
            {
                builder.Append(':').Append(serverUri.Port);
            }
            builder.Append(string.IsNullOrWhiteSpace(path) ? "/socket.io" : path);
            builder
                .Append("/?EIO=")
                .Append(eio)
                .Append("&transport=")
                .Append(ws ? "websocket" : "polling");

            if (queryParams != null)
            {
                foreach (var item in queryParams)
                {
                    builder.Append('&').Append(item.Key).Append('=').Append(item.Value);
                }
            }

            return new Uri(builder.ToString());
        }

        private static void SetSchema(bool ws, Uri serverUri, StringBuilder builder)
        {
            switch (serverUri.Scheme)
            {
                case "https" or "wss":
                    builder.Append(ws ? "wss://" : "https://");
                    break;
                case "http" or "ws":
                    builder.Append(ws ? "ws://" : "http://");
                    break;
                default:
                    throw new ArgumentException("Only supports 'http, https, ws, wss' protocol");
            }
        }
    }
}
