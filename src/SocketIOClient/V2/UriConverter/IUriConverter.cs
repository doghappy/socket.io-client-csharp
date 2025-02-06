using System;
using System.Collections.Generic;
using SocketIOClient.V2.Core;

namespace SocketIOClient.V2.UriConverter;

public interface IUriConverter
{
    Uri GetServerUri(bool ws, Uri serverUri, EngineIO eio, string path, IEnumerable<KeyValuePair<string, string>> queryParams);
}