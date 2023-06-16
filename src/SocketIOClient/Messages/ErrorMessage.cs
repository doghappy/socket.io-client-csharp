using SocketIOClient.JsonSerializer;
using SocketIOClient.Transport;
using System;
using System.Collections.Generic;


namespace SocketIOClient.Messages
{
    public class ErrorMessage<T> : IMessage
    {
        public MessageType Type => MessageType.ErrorMessage;

        public string Message { get; set; }

        public string Namespace { get; set; }

        public List<byte[]> OutgoingBytes { get; set; }

        public List<byte[]> IncomingBytes { get; set; }

        public int BinaryCount { get; }

        public EngineIO EIO { get; set; }

        public TransportProtocol Protocol { get; set; }
        public IJsonSerializer Serializer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Read(string msg)
        {
            if (EIO == EngineIO.V3)
            {
                Message = msg.Trim('"');
            }
            else
            {
                int index = msg.IndexOf('{');
                if (index > 0)
                {
                    Namespace = msg.Substring(0, index - 1);
                    msg = msg.Substring(index);
                }
                //var doc = JsonDocument.Parse(msg);
                var doc = Serializer.GetRootElement<T>(msg);
                //Message = doc.RootElement.GetProperty("message").GetString();
                Message = Serializer.GetString<T>(Serializer.GetProperty<T>(doc, "message"));
            }
        }

        public string Write()
        {
            throw new NotImplementedException();
        }
    }
}
