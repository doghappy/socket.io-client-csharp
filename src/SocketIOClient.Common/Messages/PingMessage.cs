namespace SocketIOClient.Common.Messages;

public class PingMessage : IMessage
{
    public MessageType Type => MessageType.Ping;
}