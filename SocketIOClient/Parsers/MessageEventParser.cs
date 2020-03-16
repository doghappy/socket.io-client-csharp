using Newtonsoft.Json;
using SocketIOClient.Arguments;
using System.Text.RegularExpressions;
using Websocket.Client;

namespace SocketIOClient.Parsers
{
    class MessageEventParser : Parser
    {
        public override void Parse(ParserContext ctx, ResponseMessage resMsg)
        {
            var regex = new Regex($@"^42{ctx.Namespace}\d*\[(.+)\]$");
            if (regex.IsMatch(resMsg.Text))
            {
                var groups = regex.Match(resMsg.Text).Groups;
                string text = groups[1].Value;
                var formatter = new DataFormatter();
                var data = formatter.Format(text);
                var eventHandlerArg = new ResponseArgs { RawText = resMsg.Text };
                string eventName = JsonConvert.DeserializeObject<string>(data[0]);
                if (data.Count > 1)
                    eventHandlerArg.Text = data[1];
                if (ctx.EventHandlers.ContainsKey(data[0]))
                {
                    var handlerBox = ctx.EventHandlers[data[0]];
                    handlerBox.EventHandler?.Invoke(eventHandlerArg);
                    if (handlerBox.EventHandlers != null)
                    {
                        for (int i = 0; i < handlerBox.EventHandlers.Count; i++)
                        {
                            var arg = new ResponseArgs { RawText = resMsg.Text };
                            if (i + 2 <= data.Count - 1)
                                arg.Text = data[i + 2];
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
