using SocketIOClient.Arguments;
using System.Text.RegularExpressions;
using Websocket.Client;

namespace SocketIOClient.Parsers
{
    class MessageAckParser : Parser
    {
        public override void Parse(ParserContext ctx, ResponseMessage resMsg)
        {
            var regex = new Regex($@"^43{ctx.Namespace}(\d+)\[([\s\S]*)\]$");
            if (regex.IsMatch(resMsg.Text))
            {
                var groups = regex.Match(resMsg.Text).Groups;
                int packetId = int.Parse(groups[1].Value);
                if (ctx.Callbacks.ContainsKey(packetId))
                {
                    var handler = ctx.Callbacks[packetId];
                    handler(new ResponseArgs
                    {
                        Text = groups[2].Value,
                        RawText = resMsg.Text
                    });
                    ctx.Callbacks.Remove(packetId);
                }
            }
            else if (Next != null)
            {
                Next.Parse(ctx, resMsg);
            }
        }
    }
}
