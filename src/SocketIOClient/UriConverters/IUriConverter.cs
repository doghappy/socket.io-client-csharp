using System;
using System.Collections.Generic;

namespace SocketIOClient.UriConverters
{
    public interface IUriConverter
    {
        Uri GetHandshakeUri(Uri serverUri, string path, IEnumerable<KeyValuePair<string, string>> queryParams);
        Uri GetWebSocketUri(Uri serverUri, string path, IEnumerable<KeyValuePair<string, string>> queryParams, string sid);
    }
}
