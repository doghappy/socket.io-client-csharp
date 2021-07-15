namespace SocketIOClient.Processors
{
    public class ErrorProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            string err = ctx.EioHandler.GetErrorMessage(ctx.Message);
            ctx.ErrorHandler(err);
        }
    }
}
