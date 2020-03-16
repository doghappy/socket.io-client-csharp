using Websocket.Client;

namespace SocketIOClient.Parsers
{
    class ConnectedParser : Parser
    {
        public override void Parse(ParserContext ctx, ResponseMessage resMsg)
        {
            if (resMsg.Text == "40" + ctx.Namespace)
            {
                ctx.ConnectHandler();
            }
            else if (Next != null)
            {
                Next.Parse(ctx, resMsg);
            }
        }
    }
}
