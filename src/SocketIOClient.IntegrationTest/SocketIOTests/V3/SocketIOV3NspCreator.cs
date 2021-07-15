using System.Collections.Generic;

namespace SocketIOClient.IntegrationTest.SocketIOTests.V3
{
    public class SocketIOV3NspCreator : ISocketIOCreateable
    {
        public SocketIO Create()
        {
            return new SocketIO(Url, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", Token }
                }
            });
        }

        public string Prefix => "/nsp,V3: ";
        public string Url => "http://localhost:11003/nsp";
        public string Token => "V3";
        public int EIO => 4;
    }
}
