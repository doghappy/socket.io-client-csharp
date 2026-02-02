namespace SocketIOClient.Common.Messages;

public class DisconnectedMessage : IMessage
{
    public MessageType Type => MessageType.Disconnected;
    public string? Namespace { get; set; }
}