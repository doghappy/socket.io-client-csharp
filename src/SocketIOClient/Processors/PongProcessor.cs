using SocketIOClient.EioHandler;
using System;

namespace SocketIOClient.Processors
{
    public class PongProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            var eio3Handler = ctx.SocketIO.Options.EioHandler as Eio3Handler;
            ctx.SocketIO.InvokePong(DateTime.Now - eio3Handler.PingTime);
        }
    }
}
