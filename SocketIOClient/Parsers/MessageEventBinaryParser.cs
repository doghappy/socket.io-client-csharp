using Newtonsoft.Json.Linq;
using SocketIOClient.Arguments;
using System.Text.RegularExpressions;
using Websocket.Client;

namespace SocketIOClient.Parsers
{
    class MessageEventBinaryParser : Parser
    {
        public override void Parse(ParserContext ctx, ResponseMessage resMsg)
        {
            var regex = new Regex($@"^45(\d+)-{ctx.Namespace}(\[.+\])$");
            if (regex.IsMatch(resMsg.Text))
            {
                ClearBinary(ctx);
                var groups = regex.Match(resMsg.Text).Groups;
                ctx.ReceivedBufferCount = int.Parse(groups[1].Value);
                var array = JArray.Parse(regex.Match(resMsg.Text).Groups[2].Value);
                var eventHandlerArg = new ResponseArgs { RawText = resMsg.Text };
                string eventName = array[0].Value<string>();
                if (array.Count > 1)
                    eventHandlerArg.Text = array[1].ToString();
                if (ctx.EventHandlers.ContainsKey(eventName))
                {
                    var handlerBox = ctx.EventHandlers[eventName];
                    if (handlerBox.EventHandler != null)
                    {
                        ctx.BinaryEvents.Add(new BinaryEvent
                        {
                            EventHandler = handlerBox.EventHandler,
                            ResponseArgs = eventHandlerArg
                        });
                    }
                    if (handlerBox.EventHandlers != null)
                    {
                        for (int i = 0; i < handlerBox.EventHandlers.Count; i++)
                        {
                            var arg = new ResponseArgs { RawText = resMsg.Text };
                            if (i + 2 <= array.Count - 1)
                                arg.Text = array[i + 2].ToString();
                            ctx.BinaryEvents.Add(new BinaryEvent
                            {
                                EventHandler = handlerBox.EventHandlers[i],
                                ResponseArgs = arg
                            });
                        }
                    }
                }
            }
            else if (Next != null)
            {
                Next.Parse(ctx, resMsg);
            }
        }

        private void ClearBinary(ParserContext ctx)
        {
            ctx.ReceivedBufferCount = 0;
            ctx.ReceivedBuffers.Clear();
            ctx.BinaryEvents.Clear();
        }
    }
}
