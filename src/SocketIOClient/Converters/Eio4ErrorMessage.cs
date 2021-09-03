using System;
using System.Text.Json;

namespace SocketIOClient.Converters
{
    public class Eio4ErrorMessage : ICvtMessage
    {
        public CvtMessageType Type => CvtMessageType.MessageError;

        public string Message { get; set; }

        public void Read(string msg)
        {
            var doc = JsonDocument.Parse(msg);
            Message = doc.RootElement.GetProperty("message").GetString();
        }

        public string Write()
        {
            throw new NotImplementedException();
        }
    }
}
