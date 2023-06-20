using SocketIOClient.JsonSerializer;
using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIOClient.Messages
{
    public static class MessageFactory<T>
    {
        private static IMessage CreateMessage(MessageType type)
        {
            switch (type)
            {
                case MessageType.Opened:
                    return new OpenedMessage<T>();
                case MessageType.Ping:
                    return new PingMessage<T>();
                case MessageType.Pong:
                    return new PongMessage<T>();
                case MessageType.Connected:
                    return new ConnectedMessage<T>();
                case MessageType.Disconnected:
                    return new DisconnectedMessage<T>();
                case MessageType.EventMessage:
                    return new EventMessage<T>();
                case MessageType.AckMessage:
                    return new ClientAckMessage<T>();
                case MessageType.ErrorMessage:
                    return new ErrorMessage<T>();
                case MessageType.BinaryMessage:
                    return new BinaryMessage<T>();
                case MessageType.BinaryAckMessage:
                    return new ClientBinaryAckMessage<T>();
            }
            return null;
        }

        public static IMessage CreateMessage(EngineIO eio,IJsonSerializer serializer, string msg)
        {
            var enums = Enum.GetValues(typeof(MessageType));
            foreach (MessageType item in enums)
            {
                string prefix = ((int)item).ToString();
                if (msg.StartsWith(prefix))
                {
                    IMessage result = CreateMessage(item);
                    if (result != null)
                    {
                        result.Serializer = serializer;
                        result.EIO = eio;
                        result.Read(msg.Substring(prefix.Length));
                        return result;
                    }
                }
            }
            return null;
        }

        public static OpenedMessage<T> CreateOpenedMessage(string msg, IJsonSerializer serializer)
        {
            var openedMessage = new OpenedMessage<T>();
            if (msg[0] == '0')
            {
                openedMessage.EIO = EngineIO.V4;
                openedMessage.Serializer = serializer;
                openedMessage.Read(msg.Substring(1));
            }
            else
            {
                openedMessage.EIO = EngineIO.V3;
                openedMessage.Serializer = serializer;
                int index = msg.IndexOf(':');
                openedMessage.Read(msg.Substring(index + 2));
            }
            return openedMessage;
        }
    }
}
