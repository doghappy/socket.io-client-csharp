using System;
using System.Collections.Generic;

namespace SocketIOClient.V2.UriConverter;

public interface IUriConverter
{
    Uri GetServerUri(bool ws, Uri serverUri, string path, IEnumerable<KeyValuePair<string, string>> queryParams);
}