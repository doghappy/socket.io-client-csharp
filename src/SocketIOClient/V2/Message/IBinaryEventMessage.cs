namespace SocketIOClient.V2.Message;

public interface IBinaryEventMessage : IEventMessage
{
    bool ReadyDelivery { get; }
    void Add(byte[] bytes);
}