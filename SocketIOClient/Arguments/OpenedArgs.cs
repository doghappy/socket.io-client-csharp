using System.Collections.Generic;

namespace SocketIOClient.Arguments
{
    public class OpenedArgs
    {
        public string Sid { get; set; }

        public List<string> Upgrades { get; set; }

        public int PingInterval { get; set; }

        public int PingTimeout { get; set; }
    }
}
