using System;
using System.Collections.Generic;
using SocketIO.Core;

namespace SocketIO.Client.Transport
{
    public class TransportOptions
    {
        public EngineIO EIO { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Query { get; set; }
        public object Auth { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
    }
}
