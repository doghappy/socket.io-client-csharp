using System;

namespace SocketIOClient.Packgers
{
    public class PingPackger : IUnpackable
    {
        public void Unpack(SocketIO client, string text)
        {
            if (client.Options.EIO == 4)
            {
                client.InvokePingV4(DateTime.Now);
            }
        }
    }
}
