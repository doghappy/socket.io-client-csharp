using Websocket.Client;

namespace SocketIOClient.Parsers
{
    class DisconnectedParser : Parser
    {
        public override void Parse(ParserContext ctx, ResponseMessage resMsg)
        {
            if (resMsg.Text == "41" + ctx.Namespace)
            {
                ctx.CloseHandler();
            }
            else if (Next != null)
            {
                Next.Parse(ctx, resMsg);
            }
        }
    }
}
