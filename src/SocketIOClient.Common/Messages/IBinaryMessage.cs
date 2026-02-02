namespace SocketIOClient.Common.Messages;

public interface IBinaryMessage : IMessage
{
    bool ReadyDelivery { get; }
    void Add(byte[] bytes);
}