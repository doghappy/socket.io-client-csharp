using System.Linq;
using System.Text.Json;

namespace SocketIOClient.Processors
{
    public class AckProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            if (!string.IsNullOrEmpty(ctx.Namespace) && ctx.Message.StartsWith(ctx.Namespace + ','))
            {
                ctx.Message = ctx.Message.Substring(ctx.Namespace.Length + 1);
            }
            int index = ctx.Message.IndexOf('[');
            if (index > 0)
            {
                string no = ctx.Message.Substring(0, index);
                string data = ctx.Message.Substring(index);
                if (int.TryParse(no, out int packetId))
                {
                    var doc = JsonDocument.Parse(data);
                    var array = doc.RootElement.EnumerateArray().ToList();
                    ctx.AckHandler(packetId, array);
                }
            }
        }
    }
}
