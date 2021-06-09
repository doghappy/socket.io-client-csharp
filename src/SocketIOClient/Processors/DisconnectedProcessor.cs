namespace SocketIOClient.Processors
{
    public class DisconnectedProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.SocketIO.Namespace))
            {
                if (ctx.Message == string.Empty)
                {
                    ctx.SocketIO.InvokeDisconnect("io server disconnect");
                }
            }
            else
            {
                if (ctx.Message == ctx.SocketIO.Namespace)
                {
                    ctx.SocketIO.InvokeDisconnect("io server disconnect");
                }
            }
        }
    }
}
