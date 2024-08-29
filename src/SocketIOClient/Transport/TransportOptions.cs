using System;
using System.Collections.Generic;
using SocketIO.Core;

namespace SocketIOClient.Transport
{
    public class TransportOptions
    {
        public EngineIO EIO { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Query { get; set; }
        public object Auth { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
        public Uri ServerUri { get; set; }
        public string Path { get; set; }
        public bool AutoUpgrade { get; set; }
        public IMessage OpenedMessage { get; set; }
        public string Namespace { get; set; }
    }
}
