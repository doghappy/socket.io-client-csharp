using System;
using System.Collections.Generic;
using SocketIOClient.Core;

namespace SocketIOClient.V2.Session;

public class SessionOptions
{
    public Uri ServerUri { get; set; }
    public string Path { get; set; }
    public string Namespace { get; set; }
    public IEnumerable<KeyValuePair<string, string>> Query { get; set; }
    public TimeSpan Timeout { get; set; }
    public EngineIO EngineIO { get; set; }
    public IReadOnlyDictionary<string, string> ExtraHeaders { get; set; }
}