namespace SocketIOClient.Common.Messages
{
    public interface IMessage
    {
        MessageType Type { get; }
    }
}