using System.Text.Json;

namespace SocketIOClient.Processors
{
    public class ErrorEio4Processor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            var doc= JsonDocument.Parse(ctx.Message);
            string error = doc.RootElement.GetProperty("message").GetString();
            ctx.SocketIO.InvokeError(error);
        }
    }
}
