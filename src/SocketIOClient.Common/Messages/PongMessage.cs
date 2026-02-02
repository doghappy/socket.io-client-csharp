using System;

namespace SocketIOClient.Common.Messages;

public class PongMessage : IMessage
{
    public MessageType Type => MessageType.Pong;
    public TimeSpan Duration { get; set; }
}