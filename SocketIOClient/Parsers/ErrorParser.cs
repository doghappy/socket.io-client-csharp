using SocketIOClient.Arguments;
using System.Text.RegularExpressions;
using Websocket.Client;

namespace SocketIOClient.Parsers
{
    class ErrorParser : Parser
    {
        public override void Parse(ParserContext ctx, ResponseMessage resMsg)
        {
            var regex = new Regex($@"^44{ctx.Namespace}([\s\S]*)$");
            if (regex.IsMatch(resMsg.Text))
            {
                var groups = regex.Match(resMsg.Text).Groups;
                ctx.ErrorHandler(new ResponseArgs
                {
                    Text = groups[1].Value,
                    RawText = resMsg.Text
                });
            }
            else if (Next != null)
            {
                Next.Parse(ctx, resMsg);
            }
        }
    }
}
