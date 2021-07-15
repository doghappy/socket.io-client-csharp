using System.Text.Json;

namespace SocketIOClient.Processors
{
    public class OpenedProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            if (ctx.Message.StartsWith("{\"sid\":\""))
            {
                var doc = JsonDocument.Parse(ctx.Message);
                var root = doc.RootElement;
                string sid = root.GetProperty("sid").GetString();
                int pingInterval = root.GetProperty("pingInterval").GetInt32();
                int pingTimeout = root.GetProperty("pingTimeout").GetInt32();
                ctx.OpenedHandler(sid, pingInterval, pingTimeout);
            }
        }
    }
}
