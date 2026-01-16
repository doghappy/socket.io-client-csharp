using System;

namespace SocketIOClient.Session.EngineIOAdapter;

public class EngineIOAdapterOptions
{
    public TimeSpan Timeout { get; set; }
    public string Namespace { get; set; }
    public object Auth { get; set; }
    public bool AutoUpgrade { get; set; }
}