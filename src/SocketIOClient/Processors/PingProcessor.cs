namespace SocketIOClient.Processors
{
    public class PingProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            ctx.PingHandler();
        }
    }
}
