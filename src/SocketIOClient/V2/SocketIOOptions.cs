using System;
using System.Collections.Generic;
using SocketIOClient.Core;

namespace SocketIOClient.V2;

public class SocketIOOptions
{
    public EngineIO EIO { get; set; } = EngineIO.V4;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool Reconnection { get; set; } = true;

    private int _reconnectionAttempts = 10;
    public int ReconnectionAttempts
    {
        get => _reconnectionAttempts;
        set
        {
            if (value < 1)
            {
                throw new ArgumentException("The minimum allowable number of attempts is 1");
            }
            _reconnectionAttempts = value;
        }
    }

    public int ReconnectionDelayMax { get; set; } = 5000;
    public string Path { get; set; } = "/socket.io";
    public IEnumerable<KeyValuePair<string, string>> Query { get; set; }
    public IReadOnlyDictionary<string, string> ExtraHeaders { get; set; }
}