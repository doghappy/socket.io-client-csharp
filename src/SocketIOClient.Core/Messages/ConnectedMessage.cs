namespace SocketIOClient.Core.Messages;

public class ConnectedMessage : INamespaceMessage
{
    public MessageType Type => MessageType.Connected;
    public string Namespace { get; set; }
    public string Sid { get; set; }
}