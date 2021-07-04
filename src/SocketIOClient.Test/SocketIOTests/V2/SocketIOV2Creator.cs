using System.Collections.Generic;

namespace SocketIOClient.Test.SocketIOTests.V2
{
    public class SocketIOV2Creator : ISocketIOCreateable
    {
        public SocketIO Create()
        {
            return new SocketIO(Url, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", Token }
                },
                EIO = 3
            });
        }

        public string Prefix => "V2: ";
        public string Url => "http://localhost:11002";
        public string Token => "V2";
        public int EIO => 3;
    }
}
