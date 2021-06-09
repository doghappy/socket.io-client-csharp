using System;

namespace SocketIOClient.Processors
{
    public class PingProcessor : Processor
    {
        public override async void Process(MessageContext ctx)
        {
            if (ctx.SocketIO.Options.EIO == 4)
            {
                ctx.SocketIO.InvokePing();
                DateTime pingTime = DateTime.Now;
                await ctx.SocketIO.Socket.SendMessageAsync("3");
                ctx.SocketIO.InvokePong(DateTime.Now - pingTime);
            }
        }
    }
}
