using System;

namespace SocketIOClient.Packgers
{
    public class PongPackger : IUnpackable
    {
        public void Unpack(SocketIO client, string text)
        {
            client.InvokePong(DateTime.Now - client.PingTime);
        }
    }
}
