namespace SocketIOClient.Core.Messages;

public class TypeOnlyMessage(MessageType type) : IMessage
{
    public MessageType Type { get; } = type;
}