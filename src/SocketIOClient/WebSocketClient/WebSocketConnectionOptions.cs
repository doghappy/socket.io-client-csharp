using System;
using System.Net;

namespace SocketIOClient.WebSocketClient
{
    public class WebSocketConnectionOptions
    {
        public TimeSpan ConnectionTimeout { get; set; }
        public IWebProxy Proxy { get; set; } = null;
    }
}
