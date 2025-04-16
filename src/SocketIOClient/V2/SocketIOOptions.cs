using System;

namespace SocketIOClient.V2;

public class SocketIOOptions
{
    public EngineIO EIO { get; set; }
    public TimeSpan ConnectionTimeout { get; set; }
    public bool Reconnection { get; set; }
    public int ReconnectionAttempts { get; set; }
}