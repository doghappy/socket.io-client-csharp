namespace SocketIOClient.Processors
{
    public class ErrorEio3Processor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            string error = ctx.Message.Trim('"');
            ctx.SocketIO.InvokeError(error);
        }
    }
}
