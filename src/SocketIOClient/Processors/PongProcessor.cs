namespace SocketIOClient.Processors
{
    public class PongProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            ctx.PongHandler();
        }
    }
}
