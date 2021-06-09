using SocketIOClient.Response;
using System.Text.Json;

namespace SocketIOClient.Processors
{
    public class OpenProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            if (ctx.Message.StartsWith("{\"sid\":\""))
            {
                var doc = JsonDocument.Parse(ctx.Message);
                var root = doc.RootElement;
                var response = new OpenResponse
                {
                    Sid = root.GetProperty("sid").GetString(),
                    PingInterval = root.GetProperty("pingInterval").GetInt32(),
                    PingTimeout = root.GetProperty("pingTimeout").GetInt32()
                };
                ctx.SocketIO.Open(response);
            }
        }
    }
}
