namespace SocketIOClient.Common.Messages;

public interface INamespaceMessage : IMessage
{
    string? Namespace { get; set; }
}