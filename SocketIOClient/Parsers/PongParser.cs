using System;
using Websocket.Client;

namespace SocketIOClient.Parsers
{
    class PongParser : Parser
    {
        public override void Parse(ParserContext ctx, ResponseMessage resMsg)
        {
            if (resMsg.Text == "3")
            {
                ctx.PongAt = DateTimeOffset.Now;
                ctx.PongHandler();
            }
            else if (Next != null)
            {
                Next.Parse(ctx, resMsg);
            }
        }
    }
}
