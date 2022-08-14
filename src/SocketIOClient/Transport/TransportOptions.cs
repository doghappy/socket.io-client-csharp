using System;
using System.Collections.Generic;

namespace SocketIOClient.Transport
{
    public class TransportOptions
    {
        public EngineIO EIO { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Query { get; set; }
        public string Auth { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
    }
}
