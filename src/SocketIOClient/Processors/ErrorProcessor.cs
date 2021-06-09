namespace SocketIOClient.Processors
{
    public class ErrorProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            if (ctx.SocketIO.Options.EIO == 3)
            {
                NextProcessor = new ErrorEio3Processor();
            }
            else
            {
                NextProcessor = new ErrorEio4Processor();
            }
            NextProcessor.Process(ctx);
        }
    }
}
