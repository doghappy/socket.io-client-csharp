using Newtonsoft.Json;
using SocketIOClient.Arguments;
using System.Text.RegularExpressions;
using Websocket.Client;

namespace SocketIOClient.Parsers
{
    class MessageEventBinaryParser : Parser
    {
        public override void Parse(ParserContext ctx, ResponseMessage resMsg)
        {
            var regex = new Regex($@"^45(\d+)-{ctx.Namespace}\[(.+)\]$");
            if (regex.IsMatch(resMsg.Text))
            {
                ClearBinary(ctx);
                var groups = regex.Match(resMsg.Text).Groups;
                ctx.BufferCount = int.Parse(groups[1].Value);
                string text = groups[2].Value;
                var formatter = new DataFormatter();
                var data = formatter.Format(text);
                var eventHandlerArg = new ResponseArgs { RawText = resMsg.Text };
                string eventName = JsonConvert.DeserializeObject<string>(data[0]);
                if (data.Count > 1)
                    eventHandlerArg.Text = data[1];
                if (ctx.EventHandlers.ContainsKey(data[0]))
                {
                    var handlerBox = ctx.EventHandlers[data[0]];
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
                            if (i + 2 <= data.Count - 1)
                                arg.Text = data[i + 2];
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
            ctx.BufferCount = 0;
            ctx.BinaryEvents.Clear();
        }
    }
}
