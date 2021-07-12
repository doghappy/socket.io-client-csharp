using System.Collections.Generic;

namespace SocketIOClient.Test.SocketIOTests.V4
{
    public class SocketIOV4Creator : ISocketIOCreateable
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

        public string Prefix => "V4: ";
        public string Url => "http://localhost:11004";
        public string Token => "V4";
        public int EIO => 4;
    }
}
