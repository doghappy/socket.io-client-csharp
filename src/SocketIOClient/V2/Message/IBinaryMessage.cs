namespace SocketIOClient.V2.Message;

public interface IBinaryMessage : IMessage
{
    bool ReadyDelivery { get; }
    void Add(byte[] bytes);
}