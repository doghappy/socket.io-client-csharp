using Websocket.Client;

namespace SocketIOClient.Parsers
{
    abstract class Parser
    {
        public Parser Next { get; set; }

        public abstract void Parse(ParserContext ctx, ResponseMessage resMsg);
    }
}
