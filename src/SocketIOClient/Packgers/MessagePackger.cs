using SocketIOClient.EventArguments;
using System;

namespace SocketIOClient.Packgers
{
    public class MessagePackger : IUnpackable
    {
        public void Unpack(SocketIO client, string text)
        {
            string identity = text[0].ToString();
            string content = text.Substring(1);
            if (Enum.TryParse(identity, out SocketIOProtocol protocol))
            {
                IUnpackable unpackger = null;
                switch (protocol)
                {
                    case SocketIOProtocol.Connect:
                        unpackger = new MessageConnectedPackger();
                        break;
                    case SocketIOProtocol.Disconnect:
                        unpackger = new MessageDisconnectedPackger();
                        break;
                    case SocketIOProtocol.Event:
                        unpackger = new MessageEventPackger();
                        break;
                    case SocketIOProtocol.Ack:
                        unpackger = new MessageAckPackger();
                        break;
                    case SocketIOProtocol.Error:
                        unpackger = new MessageErrorPackger();
                        break;
                    case SocketIOProtocol.BinaryEvent:
                        unpackger = new MessageBinaryEventPackger();
                        break;
                    case SocketIOProtocol.BinaryAck:
                        unpackger = new MessageBinaryAckPackger();
                        break;
                }
                if (unpackger != null)
                {
                    if (protocol == SocketIOProtocol.Event || protocol == SocketIOProtocol.BinaryEvent)
                    {
                        var receivedEvent = unpackger as IReceivedEvent;
                        receivedEvent.OnEnd += () =>
                        {
                            client.InvokeReceivedEvent(new ReceivedEventArgs
                            {
                                Event = receivedEvent.EventName,
                                Response = receivedEvent.Response
                            });
                        };
                    }
                    unpackger.Unpack(client, content);
                }
            }
        }
    }
}
