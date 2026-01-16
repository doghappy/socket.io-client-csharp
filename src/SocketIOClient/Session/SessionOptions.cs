using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using SocketIOClient.Core;

namespace SocketIOClient.Session;

public class SessionOptions
{
    public Uri ServerUri { get; set; }
    public string Path { get; set; }
    public string Namespace { get; set; }
    public NameValueCollection Query { get; set; }
    public TimeSpan Timeout { get; set; }
    public EngineIO EngineIO { get; set; }
    public IReadOnlyDictionary<string, string> ExtraHeaders { get; set; }
    public object Auth { get; set; }
    public bool AutoUpgrade { get; set; }
    public string Sid { get; set; }
}