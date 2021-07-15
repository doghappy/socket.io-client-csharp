namespace SocketIOClient.Processors
{
    public class DisconnectedProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.Namespace))
            {
                if (ctx.Message == string.Empty)
                {
                    ctx.DisconnectedHandler();
                }
            }
            else
            {
                if (ctx.Message.StartsWith(ctx.Namespace))
                {
                    ctx.DisconnectedHandler();
                }
            }
        }
    }
}
