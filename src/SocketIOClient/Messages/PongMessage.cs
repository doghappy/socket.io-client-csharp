using SocketIOClient.JsonSerializer;
using SocketIOClient.Transport;
using System;
using System.Collections.Generic;

namespace SocketIOClient.Messages
{
    public class PongMessage<T> : IMessage
    {
        public MessageType Type => MessageType.Pong;

        public List<byte[]> OutgoingBytes { get; set; }

        public List<byte[]> IncomingBytes { get; set; }

        public int BinaryCount { get; }

        public EngineIO EIO { get; set; }

        public TransportProtocol Protocol { get; set; }

        public TimeSpan Duration { get; set; }
        public IJsonSerializer Serializer { get; set; }

        public void Read(string msg)
        {
        }

        public string Write() => "3";
    }
}
