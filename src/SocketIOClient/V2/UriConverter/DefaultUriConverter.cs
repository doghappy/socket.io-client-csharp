using System;
using System.Collections.Generic;
using System.Web;

namespace SocketIOClient.V2.UriConverter
{
    public class DefaultUriConverter : IUriConverter
    {
        private const string DefaultPath = "/socket.io/";

        public Uri GetServerUri(
            bool ws,
            Uri serverUri,
            string path,
            IEnumerable<KeyValuePair<string, string>> queryParams,
            int eio)
        {
            var scheme = GetScheme(ws, serverUri);

            var uriBuilder = new UriBuilder(serverUri)
            {
                Scheme = scheme,
                Port = serverUri.IsDefaultPort ? -1 : serverUri.Port,
                Path = string.IsNullOrWhiteSpace(path) ? DefaultPath : path
            };

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["EIO"] = eio.ToString();
            query["transport"] = ws ? "websocket" : "polling";

            if (queryParams != null)
            {
                foreach (var item in queryParams)
                {
                    query[item.Key] = item.Value;
                }
            }

            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri;
        }

        private static string GetScheme(bool ws, Uri serverUri) => serverUri.Scheme switch
        {
            "https" or "wss" => ws ? "wss" : "https",
            "http" or "ws" => ws ? "ws" : "http",
            _ => throw new ArgumentException("Only supports 'http, https, ws, wss' protocol")
        };
    }
}
