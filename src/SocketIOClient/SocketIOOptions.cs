using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;

namespace SocketIOClient
{
    public sealed class SocketIOOptions
    {
        public SocketIOOptions()
        {
            RandomizationFactor = new Random().NextDouble();
        }

        public string Path { get; set; } = "/socket.io";

        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(20);
        public IWebProxy Proxy { get; set; } = null;

        public Dictionary<string, string> Query { get; set; }

        public bool Reconnection { get; set; } = true;

        public int ReconnectionDelay { get; set; } = 1000;
        public int ReconnectionDelayMax { get; set; } = 5000;

        public SslProtocols EnabledSslProtocols { get; set; } = SslProtocols.None;

        double _randomizationFactor;
        public double RandomizationFactor
        {
            get => _randomizationFactor;
            set
            {
                if (value >= 0 && value <= 1)
                {
                    _randomizationFactor = value;
                }
                else
                {
                    throw new ArgumentException($"{nameof(RandomizationFactor)} should be greater than or equal to 0.0, and less than 1.0.");
                }
            }
        }
    }
}
