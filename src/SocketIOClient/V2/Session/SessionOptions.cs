using System;
using System.Collections.Generic;
using SocketIOClient.V2.Core;

namespace SocketIOClient.V2.Session;

public class SessionOptions
{
    public Uri ServerUri { get; set; }
    public EngineIO EngineIO { get; set; }
    public string Path { get; set; }
    public IEnumerable<KeyValuePair<string, string>> Query { get; set; }
}