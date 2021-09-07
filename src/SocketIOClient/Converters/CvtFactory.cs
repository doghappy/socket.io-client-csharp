using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIOClient.Converters
{
    public static class CvtFactory
    {
        public static ICvtMessage GetByType(int eio, CvtMessageType type)
        {
            switch (type)
            {
                case CvtMessageType.Opened:
                    return new OpenedMessage();
                case CvtMessageType.Ping:
                    return new PingMessage();
                case CvtMessageType.Pong:
                    return new PongMessage();
                case CvtMessageType.Connected:
                    if (eio == 3)
                        return new Eio3ConnectedMessage();
                    return new Eio4ConnectedMessage();
                case CvtMessageType.Disconnected:
                    return new DisconnectedMessage();
                case CvtMessageType.EventMessage:
                    return new EventMessage();
                case CvtMessageType.AckMessage:
                    return new ServerAckMessage();
                case CvtMessageType.ErrorMessage:
                    if (eio == 3)
                        return new Eio3ErrorMessage();
                    return new Eio4ErrorMessage();
                case CvtMessageType.BinaryMessage:
                    return new BinaryMessage();
                case CvtMessageType.BinaryAckMessage:
                    return new ServerBinaryAckMessage();
            }
            return null;
        }

        public static ICvtMessage GetMessage(int eio, string msg)
        {
            var enums = Enum.GetValues(typeof(CvtMessageType));
            foreach (CvtMessageType item in enums)
            {
                string prefix = ((int)item).ToString();
                if (msg.StartsWith(prefix))
                {
                    ICvtMessage result = GetByType(eio, item);
                    if (result != null)
                    {
                        result.Read(msg.Substring(prefix.Length));
                        return result;
                    }
                }
            }
            return null;
        }
    }
}
