using System.Linq;
using System.Text.Json;

namespace SocketIOClient.Processors
{
    public class AckProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            if (!string.IsNullOrEmpty(ctx.SocketIO.Namespace) && ctx.Message.StartsWith(ctx.SocketIO.Namespace))
            {
                ctx.Message = ctx.Message.Substring(ctx.SocketIO.Namespace.Length);
            }
            int index = ctx.Message.IndexOf('[');
            if (index > 0)
            {
                string no = ctx.Message.Substring(0, index);
                string data = ctx.Message.Substring(index);
                if (int.TryParse(no, out int packetId))
                {
                    if (ctx.SocketIO.Acks.ContainsKey(packetId))
                    {
                        var doc = JsonDocument.Parse(data);
                        var array = doc.RootElement.EnumerateArray().ToList();
                        var response = new SocketIOResponse(array, ctx.SocketIO);
                        ctx.SocketIO.Acks[packetId](response);
                        ctx.SocketIO.Acks.Remove(packetId);
                    }
                }
            }
        }
    }
}
