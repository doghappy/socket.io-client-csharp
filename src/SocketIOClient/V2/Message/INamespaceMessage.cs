namespace SocketIOClient.V2.Message;

public interface INamespaceMessage : IMessage
{
    string Namespace { get; set; }
}