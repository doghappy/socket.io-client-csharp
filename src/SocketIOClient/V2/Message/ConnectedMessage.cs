namespace SocketIOClient.V2.Message;

public class ConnectedMessage : IMessage
{
    public MessageType Type => MessageType.Connected;
    public string Namespace { get; set; }
    public string Sid { get; set; }
}