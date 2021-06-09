using System.Collections.Generic;

namespace SocketIOClient.Test.SocketIOTests.V4
{
    public class ScoketIOV4NspCreator : ISocketIOCreateable
    {
        public SocketIO Create()
        {
            return new SocketIO(Url, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", Token }
                },
                EIO = EIO
            });
        }

        public string Prefix => "/nsp,V4: ";
        public string Url => "http://localhost:11004/nsp";
        public string Token => "V4";
        public int EIO => 4;
    }
}
