using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using SocketIOClient.Core;

namespace SocketIOClient;

public class SocketIOOptions
{
    public EngineIO EIO { get; set; } = EngineIO.V4;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool Reconnection { get; set; } = true;

    public TransportProtocol Transport { get; set; }

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

    private string? _path;
    public string? Path
    {
        get => _path;
        set => _path = $"/{value!.Trim('/')}/";
    }

    public NameValueCollection? Query { get; set; }
    public IReadOnlyDictionary<string, string>? ExtraHeaders { get; set; }

    public object? Auth { get; set; }
    public bool AutoUpgrade { get; set; } = true;
}