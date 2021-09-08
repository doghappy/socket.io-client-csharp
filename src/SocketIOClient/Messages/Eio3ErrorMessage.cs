using System;

namespace SocketIOClient.Messages
{
    public class Eio3ErrorMessage : IMessage
    {
        public MessageType Type => MessageType.ErrorMessage;

        public string Message { get; set; }

        public void Read(string msg)
        {
            Message = msg.Trim('"');
        }

        public string Write()
        {
            throw new NotImplementedException();
        }
    }
}
