namespace SocketIOClient.Core.Messages;

public interface INamespaceMessage : IMessage
{
    string Namespace { get; set; }
}