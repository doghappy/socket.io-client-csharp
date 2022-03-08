using System.Collections.Generic;

namespace SocketIOClient.Test.SocketIOTests.V3
{
    public class SocketIOV3NspCreator : ISocketIOCreateable
    {
        public SocketIO Create(bool reconnection = false)
        {
            return new SocketIO(Url, new SocketIOOptions
            {
                Reconnection = reconnection,
                AutoUpgrade = false,
                Transport = Transport.TransportProtocol.WebSocket,
                Query = new Dictionary<string, string>
                {
                    { "token", Token }
                }
            });
        }

        public string Prefix => "/nsp,V3: ";
        public string Url => "http://localhost:11003/nsp";
        public string Token => "V3NSP";
        public int EIO => 4;
    }
}
