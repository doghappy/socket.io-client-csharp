using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIOClient.Processors
{
    public class SocketIOProtocolProcessor : Processor
    {
        public override void Process(MessageContext ctx)
        {
            string sio = ctx.Message[0].ToString();
            string content = ctx.Message.Substring(1);
            if (Enum.TryParse(sio, out SocketIOProtocol protocol))
            {
                switch (protocol)
                {
                    case SocketIOProtocol.Connect:
                        NextProcessor = new ConnectedProcessor();
                        break;
                    case SocketIOProtocol.Disconnect:
                        NextProcessor = new DisconnectedProcessor();
                        break;
                    case SocketIOProtocol.Event:
                        NextProcessor = new EventProcessor();
                        break;
                    case SocketIOProtocol.Ack:
                        NextProcessor = new AckProcessor();
                        break;
                    case SocketIOProtocol.Error:
                        NextProcessor = new ErrorProcessor();
                        break;
                    case SocketIOProtocol.BinaryEvent:
                        NextProcessor = new BinaryEventProcessor();
                        break;
                    case SocketIOProtocol.BinaryAck:
                        NextProcessor = new BinaryAckProcessor();
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
