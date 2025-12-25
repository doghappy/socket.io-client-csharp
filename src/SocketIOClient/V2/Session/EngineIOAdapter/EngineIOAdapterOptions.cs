using System;

namespace SocketIOClient.V2.Session.EngineIOAdapter;

public class EngineIOAdapterOptions
{
    public TimeSpan Timeout { get; set; }
    public string Namespace { get; set; }
    public object Auth { get; set; }
}