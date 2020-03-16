using Newtonsoft.Json;
using SocketIOClient.Arguments;
using Websocket.Client;

namespace SocketIOClient.Parsers
{
    class OpenedParser : Parser
    {
        public override void Parse(ParserContext ctx, ResponseMessage resMsg)
        {
            if (resMsg.Text.StartsWith("0{\"sid\":\""))
            {
                string message = resMsg.Text.TrimStart('0');
                var args = JsonConvert.DeserializeObject<OpenedArgs>(message);
                ctx.OpenHandler(args);
            }
            else if (Next != null)
            {
                Next.Parse(ctx, resMsg);
            }
        }
    }
}
