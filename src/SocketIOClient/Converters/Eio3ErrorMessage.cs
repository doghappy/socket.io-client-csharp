using System;

namespace SocketIOClient.Converters
{
    public class Eio3ErrorMessage : ICvtMessage
    {
        public CvtMessageType Type => CvtMessageType.ErrorMessage;

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
