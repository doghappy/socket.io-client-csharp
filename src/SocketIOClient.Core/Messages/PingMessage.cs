namespace SocketIOClient.Core.Messages;

public class PingMessage : IMessage
{
    public MessageType Type => MessageType.Ping;
}