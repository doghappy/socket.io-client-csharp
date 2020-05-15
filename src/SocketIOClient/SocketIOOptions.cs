using System;
using System.Collections.Generic;

namespace SocketIOClient
{
    public class SocketIOOptions
    {
        public string Path { get; set; } = "/socket.io";

        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(20);

        public Dictionary<string, string> Query { get; set; }
    }
}
