namespace SocketIOClient.Processors
{
    public class ConnectedProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            ctx.SocketIO.Options.EioHandler.Unpack(ctx.SocketIO, ctx.Message);
        }
    }
}
