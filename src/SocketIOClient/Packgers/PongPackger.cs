using SocketIOClient.EioHandler;
using System;

namespace SocketIOClient.Packgers
{
    public class PongPackger : IUnpackable
    {
        public void Unpack(SocketIO client, string text)
        {
            var v3 = client.Options.EioHandler as Eio3Handler;
            client.InvokePong(DateTime.Now - v3.PingTime);
        }
    }
}
