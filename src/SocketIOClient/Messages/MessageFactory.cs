using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIOClient.Messages
{
    public static class MessageFactory
    {
        public static IMessage GetByType(int eio, MessageType type)
        {
            switch (type)
            {
                case MessageType.Opened:
                    return new OpenedMessage();
                case MessageType.Ping:
                    return new PingMessage();
                case MessageType.Pong:
                    return new PongMessage();
                case MessageType.Connected:
                    if (eio == 3)
                        return new Eio3ConnectedMessage();
                    return new ConnectedMessage();
                case MessageType.Disconnected:
                    return new DisconnectedMessage();
                case MessageType.EventMessage:
                    return new EventMessage();
                case MessageType.AckMessage:
                    return new ServerAckMessage();
                case MessageType.ErrorMessage:
                    if (eio == 3)
                        return new Eio3ErrorMessage();
                    return new Eio4ErrorMessage();
                case MessageType.BinaryMessage:
                    return new BinaryMessage();
                case MessageType.BinaryAckMessage:
                    return new ServerBinaryAckMessage();
            }
            return null;
        }

        public static IMessage GetEio4Message(string msg)
        {
            //var enums = Enum.GetValues(typeof(MessageType));
            //foreach (MessageType item in enums)
            //{
            //    string prefix = ((int)item).ToString();
            //    if (msg.StartsWith(prefix))
            //    {
            //        IMessage result = GetByType(eio, item);
            //        if (result != null)
            //        {
            //            result.Read(msg.Substring(prefix.Length));
            //            return result;
            //        }
            //    }
            //}
            //return null;
            return null;
        }

        public static IMessage GetEio3WebSocketMessage(string msg)
        {
            return null;
        }

        public static IMessage GetEio3HttpMessage(string msg)
        {
            //2:4027:44/nsp9,"Invalid namespace"
            //2:407:40/nsp,
            if (msg.StartsWith("2:40"))
            {
                if (msg == "2:40")
                {

                }
                else
                {

                }
            }
            return null;
            //int index = msg.IndexOf(':');
            //if (index > -1)
            //{
            //    int length = int.Parse(msg.Substring(0, index));
            //int start = msg.Substring(index + 1)
            //}
        }
    }
}
