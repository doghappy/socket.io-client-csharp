using System;
using System.Collections.Generic;

namespace SocketIOClient.V2.Session;

public class SessionOptions
{
    public Uri ServerUri { get; set; }
    public string Path { get; set; }
    public IEnumerable<KeyValuePair<string, string>> Query { get; set; }
    public TimeSpan Timeout { get; set; }
}