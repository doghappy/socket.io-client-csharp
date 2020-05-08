using Newtonsoft.Json.Linq;
using SocketIOClient.Arguments;
using System.Text.RegularExpressions;
using Websocket.Client;

namespace SocketIOClient.Parsers
{
    class MessageEventParser : Parser
    {
        public override void Parse(ParserContext ctx, ResponseMessage resMsg)
        {
            var regex = new Regex($@"^42{ctx.Namespace}\d*(\[.+\])$");
            if (regex.IsMatch(resMsg.Text))
            {
                var array = JArray.Parse(regex.Match(resMsg.Text).Groups[1].Value);
                var eventHandlerArg = new ResponseArgs { RawText = resMsg.Text };
                string eventName = array[0].Value<string>();
                if (array.Count > 1)
                    eventHandlerArg.Text = array[1].ToString();
                if (ctx.EventHandlers.ContainsKey(eventName))
                {
                    var handlerBox = ctx.EventHandlers[eventName];
                    handlerBox.EventHandler?.Invoke(eventHandlerArg);
                    if (handlerBox.EventHandlers != null)
                    {
                        for (int i = 0; i < handlerBox.EventHandlers.Count; i++)
                        {
                            var arg = new ResponseArgs { RawText = resMsg.Text };
                            if (i + 2 <= array.Count - 1)
                                arg.Text = array[i + 2].ToString();
                            handlerBox.EventHandlers[i](arg);
                        }
                    }
                }
                else
                {
                    ctx.UncaughtHandler(eventName, eventHandlerArg);
                }
                ctx.ReceiveHandler(eventName, eventHandlerArg);
            }
            else if (Next != null)
            {
                Next.Parse(ctx, resMsg);
            }
        }
    }
}
