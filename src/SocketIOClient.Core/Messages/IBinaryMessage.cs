namespace SocketIOClient.Core.Messages;

public interface IBinaryMessage : IMessage
{
    bool ReadyDelivery { get; }
    void Add(byte[] bytes);
}