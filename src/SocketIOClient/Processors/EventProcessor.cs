using System.Linq;
using System.Text.Json;

namespace SocketIOClient.Processors
{
    public class EventProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            if (!string.IsNullOrEmpty(ctx.Namespace) && ctx.Message.StartsWith(ctx.Namespace + ','))
            {
                ctx.Message = ctx.Message.Substring(ctx.Namespace.Length + 1);
            }
            int index = ctx.Message.IndexOf('[');
            string id = null;
            if (index > 0)
            {
                id = ctx.Message.Substring(0, index);
                ctx.Message = ctx.Message.Substring(index);
            }
            var array = JsonDocument.Parse(ctx.Message).RootElement.EnumerateArray().ToList();
            string eventName = array[0].GetString();
            array.RemoveAt(0);
            int.TryParse(id, out int packetId);
            ctx.EventReceivedHandler(packetId, eventName, array);
        }
    }
}
