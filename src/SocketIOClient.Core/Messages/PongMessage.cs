using System;

namespace SocketIOClient.Core.Messages;

public class PongMessage : IMessage
{
    public MessageType Type => MessageType.Pong;
    public TimeSpan Duration { get; set; }
}