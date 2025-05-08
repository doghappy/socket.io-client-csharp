using System;
using System.Collections.Generic;

namespace SocketIOClient.V2;

public class SocketIOOptions
{
    // TODO: what will happen if user set an invalid value?
    public EngineIO EIO { get; set; } = EngineIO.V4;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool Reconnection { get; set; } = true;
    public int ReconnectionAttempts { get; set; } = 10;
    public int ReconnectionDelayMax { get; set; } = 5000;
    public string Path { get; set; } = "/socket.io";
    public IEnumerable<KeyValuePair<string, string>> Query { get; set; }
}