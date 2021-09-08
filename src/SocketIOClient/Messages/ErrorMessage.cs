using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SocketIOClient.Messages
{
    public class ErrorMessage : IMessage
    {
        public MessageType Type => MessageType.ErrorMessage;

        public string Message { get; set; }

        public ICollection<byte[]> OutgoingBytes { get; set; }

        public ICollection<byte[]> IncomingBytes { get; }

        public int BinaryCount { get; }

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
