namespace SocketIOClient.Core.Messages
{
    public interface IMessage
    {
        MessageType Type { get; }
    }
}