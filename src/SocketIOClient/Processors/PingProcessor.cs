using System;
using System.Diagnostics;

namespace SocketIOClient.Processors
{
    public class PingProcessor : Processor
    {
        public override async void Process(MessageContext ctx)
        {
            if (ctx.SocketIO.Options.EIO == 4)
            {
                try
                {
                    ctx.SocketIO.InvokePing();
                    DateTime pingTime = DateTime.Now;
                    await ctx.SocketIO.Socket.SendMessageAsync("3");
                    ctx.SocketIO.InvokePong(DateTime.Now - pingTime);
                }
                catch (System.Net.WebSockets.WebSocketException e)
                {
                    ctx.SocketIO.InvokeDisconnect(e.Message);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
                
            }
        }
    }
}
