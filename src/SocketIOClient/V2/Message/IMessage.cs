namespace SocketIOClient.V2.Message;

public interface IMessage
{
    MessageType Type { get; }
}