namespace SocketIOClient.Processors
{
    public class ConnectedProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            var result = ctx.EioHandler.CheckConnection(ctx.Namespace, ctx.Message);
            ctx.ConnectedHandler(result);
        }
    }
}
