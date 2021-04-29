using SocketIOClient.EioHandler;
using System;
using System.Collections.Generic;

namespace SocketIOClient
{
    public sealed class SocketIOOptions
    {
        public SocketIOOptions()
        {
            RandomizationFactor = new Random().NextDouble();
            EIO = 3;
        }

        public string Path { get; set; } = "/socket.io";

        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(20);

        public Dictionary<string, string> Query { get; set; }

        /// <summary>
        /// Whether to allow reconnection if accidentally disconnected
        /// </summary>
        public bool Reconnection { get; set; } = true;

        public int ReconnectionDelay { get; set; } = 1000;
        public int ReconnectionDelayMax { get; set; } = 5000;

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

        /// <summary>
        /// Allow reconnection when the first connection fails.
        /// Generally speaking, the usage scenario is if the server starts later than the client.
        /// </summary>
        public bool AllowedRetryFirstConnection { get; set; }

        int eio;
        public int EIO
        {
            get => eio;
            set
            {
                if (eio != value)
                {
                    eio = value;
                    if (EioHandler is Eio3Handler)
                    {
                        var v3 = EioHandler as Eio3Handler;
                        v3.StopPingInterval();
                    }
                    EioHandler = EioHandlerFactory.GetHandler(value);
                }
            }
        }

        internal IEioHandler EioHandler { get; set; }
    }
}
