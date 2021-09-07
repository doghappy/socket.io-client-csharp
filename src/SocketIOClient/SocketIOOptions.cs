using SocketIOClient.EioHandler;
using SocketIOClient.Transport;
using System;
using System.Collections.Generic;

namespace SocketIOClient
{
    public sealed class SocketIOOptions
    {
        public SocketIOOptions()
        {
            RandomizationFactor = new Random().NextDouble();
            EIO = 4;
            ReconnectionDelay = 1000;
            ReconnectionDelayMax = 5000;
            ReconnectionAttempts = int.MaxValue;
            Path = "/socket.io";
            ConnectionTimeout = TimeSpan.FromSeconds(20);
            Reconnection = true;
        }

        public string Path { get; set; }

        public TimeSpan ConnectionTimeout { get; set; }

        public Dictionary<string, string> Query { get; set; }

        /// <summary>
        /// Whether to allow reconnection if accidentally disconnected
        /// </summary>
        public bool Reconnection { get; set; }

        public double ReconnectionDelay { get; set; }
        public int ReconnectionDelayMax { get; set; }
        public int ReconnectionAttempts { get; set; }

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

        int eio;
        public int EIO
        {
            get => eio;
            set
            {
                if (eio != value)
                {
                    eio = value;
                    EioHandler = EioHandlerFactory.GetHandler(value);
                }
            }
        }

        internal IEioHandler EioHandler { get; set; }
    }
}
