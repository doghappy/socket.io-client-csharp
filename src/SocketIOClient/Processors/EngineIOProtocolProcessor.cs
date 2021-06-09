using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIOClient.Processors
{
    public class EngineIOProtocolProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            string eio = ctx.Message[0].ToString();
            string content = ctx.Message.Substring(1);
            if (Enum.TryParse(eio, out EngineIOProtocol protocol))
            {
                switch (protocol)
                {
                    case EngineIOProtocol.Open:
                        NextProcessor = new OpenProcessor();
                        break;
                    case EngineIOProtocol.Ping:
                        NextProcessor = new PingProcessor();
                        break;
                    case EngineIOProtocol.Pong:
                        NextProcessor = new PongProcessor();
                        break;
                    case EngineIOProtocol.Message:
                        NextProcessor = new SocketIOProtocolProcessor();
                        break;
                }
                if (NextProcessor != null)
                {
                    ctx.Message = content;
                    NextProcessor.Process(ctx);
                }
            }
        }
    }
}
