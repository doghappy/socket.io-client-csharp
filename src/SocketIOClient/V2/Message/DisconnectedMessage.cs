namespace SocketIOClient.V2.Message;

public class DisconnectedMessage : IMessage
{
    public MessageType Type => MessageType.Disconnected;
    public string Namespace { get; set; }
}