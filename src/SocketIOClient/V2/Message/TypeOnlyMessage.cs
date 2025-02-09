namespace SocketIOClient.V2.Message;

public class TypeOnlyMessage(MessageType type) : IMessage
{
    public MessageType Type { get; } = type;
}