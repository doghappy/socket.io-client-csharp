using System.Linq;
using System.Text.Json;

namespace SocketIOClient.Processors
{
    public class EventProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            if (!string.IsNullOrEmpty(ctx.SocketIO.Namespace) && ctx.Message.StartsWith(ctx.SocketIO.Namespace))
            {
                ctx.Message = ctx.Message.Substring(ctx.SocketIO.Namespace.Length);
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
            var response = new SocketIOResponse(array, ctx.SocketIO);
            if (int.TryParse(id, out int packetId))
            {
                response.PacketId = packetId;
            }
            foreach (var item in ctx.SocketIO.OnAnyHandlers)
            {
                item(eventName, response);
            }
            ctx.SocketIO.InvokeReceivedEvent(new EventArguments.ReceivedEventArgs
            {
                Event = eventName,
                Response = response
            });
            if (ctx.SocketIO.Handlers.ContainsKey(eventName))
            {
                ctx.SocketIO.Handlers[eventName](response);
            }
        }
    }
}
