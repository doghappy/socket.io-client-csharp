using Websocket.Client;

namespace SocketIOClient.Parsers
{
    class PongParser : Parser
    {
        public override void Parse(ParserContext ctx, ResponseMessage resMsg)
        {
            if (resMsg.Text == "3")
            {
                ctx.PongHandler();
            }
            else if (Next != null)
            {
                Next.Parse(ctx, resMsg);
            }
        }
    }
}
