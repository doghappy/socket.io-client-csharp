namespace SocketIOClient.V2.Message;

public class ErrorMessage : INamespaceMessage
{
    public MessageType Type => MessageType.Error;
    public string Namespace { get; set; }
    public string Error { get; set; }
}