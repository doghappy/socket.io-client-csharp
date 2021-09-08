using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIOClient.UriConverters
{
    public class UriConverter : IUriConverter
    {
        public Uri GetHandshakeUri(Uri serverUri, string path, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var builder = new StringBuilder();
            if (serverUri.Scheme == "https" || serverUri.Scheme == "wss")
            {
                builder.Append("https://");
            }
            else if (serverUri.Scheme == "http" || serverUri.Scheme == "ws")
            {
                builder.Append("http://");
            }
            else
            {
                throw new ArgumentException("Only supports 'http, https, ws, wss' protocol");
            }
            builder.Append(serverUri.Host);
            if (!serverUri.IsDefaultPort)
            {
                builder.Append(":").Append(serverUri.Port);
            }
            if (string.IsNullOrEmpty(path))
            {
                builder.Append("/socket.io");
            }
            else
            {
                builder.Append(path);
            }
            builder.Append("/?EIO=4&transport=polling");
            if (queryParams != null)
            {
                foreach (var item in queryParams)
                {
                    builder.Append('&').Append(item.Key).Append('=').Append(item.Value);
                }
            }

            return new Uri(builder.ToString());
        }

        public Uri GetWebSocketUri(Uri serverUri, string path, IEnumerable<KeyValuePair<string, string>> queryParams, string sid)
        {
            var builder = new StringBuilder();
            if (serverUri.Scheme == "https" || serverUri.Scheme == "wss")
            {
                builder.Append("wss://");
            }
            else if (serverUri.Scheme == "http" || serverUri.Scheme == "ws")
            {
                builder.Append("ws://");
            }
            else
            {
                throw new ArgumentException("Only supports 'http, https, ws, wss' protocol");
            }
            builder.Append(serverUri.Host);
            if (!serverUri.IsDefaultPort)
            {
                builder.Append(":").Append(serverUri.Port);
            }
            if (string.IsNullOrEmpty(path))
            {
                builder.Append("/socket.io");
            }
            else
            {
                builder.Append(path);
            }
            builder.Append("/?EIO=4&transport=polling");
            if (queryParams != null)
            {
                foreach (var item in queryParams)
                {
                    builder.Append('&').Append(item.Key).Append('=').Append(item.Value);
                }
            }
            builder.Append("&sid=").Append(sid);

            return new Uri(builder.ToString());
        }
    }
}
